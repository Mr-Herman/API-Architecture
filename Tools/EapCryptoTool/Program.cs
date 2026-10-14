using System.Security.Cryptography;
using System.Text;

namespace EapCryptoTool;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new CryptoForm());
    }
}

internal sealed class CryptoForm : Form
{
    private const string EncryptedPrefix = "enc:v1:";
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private const int KeySize = 32;

    private static readonly byte[] AdditionalData =
        Encoding.UTF8.GetBytes("EAP.Database.ConnectionString.v1");

    private readonly TextBox _keyTextBox = new();
    private readonly TextBox _plainTextBox = new();
    private readonly TextBox _encryptedTextBox = new();
    private readonly Label _statusLabel = new();

    public CryptoForm()
    {
        Text = "EAP 连接串加密/解密工具";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(820, 620);
        Size = new Size(920, 680);
        Font = new Font("Microsoft YaHei UI", 10F);

        BuildLayout();
    }

    private void BuildLayout()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            ColumnCount = 1,
            RowCount = 9
        };

        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 45F));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 55F));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        layout.Controls.Add(CreateLabel("AES 密钥（Base64，32 字节）"));

        _keyTextBox.Dock = DockStyle.Top;
        _keyTextBox.PlaceholderText = "例如：k936H1qOomIVe5mthVn96Ad7MpxVsMaB4aDkCthRcyw=";
        layout.Controls.Add(_keyTextBox);

        layout.Controls.Add(CreateLabel("明文连接串"));
        ConfigureMultilineTextBox(_plainTextBox);
        layout.Controls.Add(_plainTextBox);

        var encryptButton = CreateButton("加密 ↓", EncryptButtonClick);
        layout.Controls.Add(CreateButtonPanel(encryptButton));

        layout.Controls.Add(CreateLabel("密文（enc:v1:）"));
        ConfigureMultilineTextBox(_encryptedTextBox);
        layout.Controls.Add(_encryptedTextBox);

        var decryptButton = CreateButton("解密 ↑", DecryptButtonClick);
        var copyButton = CreateButton("复制密文", CopyEncryptedButtonClick);
        var clearButton = CreateButton("清空", ClearButtonClick);
        layout.Controls.Add(CreateButtonPanel(decryptButton, copyButton, clearButton));

        _statusLabel.AutoSize = true;
        _statusLabel.ForeColor = Color.DimGray;
        _statusLabel.Text = "密钥和连接串仅在当前窗口内使用，不会保存。";
        layout.Controls.Add(_statusLabel);

        Controls.Add(layout);
    }

    private void EncryptButtonClick(object? sender, EventArgs e)
    {
        RunSafely(() =>
        {
            var key = ReadKey();
            var connectionString = _plainTextBox.Text.Trim();

            if (connectionString.Length == 0)
            {
                throw new InvalidOperationException("请输入明文连接串。");
            }

            _encryptedTextBox.Text = Encrypt(connectionString, key);
            SetSuccess("加密完成。");
        });
    }

    private void DecryptButtonClick(object? sender, EventArgs e)
    {
        RunSafely(() =>
        {
            var key = ReadKey();
            var encryptedValue = _encryptedTextBox.Text.Trim();

            if (encryptedValue.Length == 0)
            {
                throw new InvalidOperationException("请输入密文。");
            }

            _plainTextBox.Text = Decrypt(encryptedValue, key);
            SetSuccess("解密完成。");
        });
    }

    private void CopyEncryptedButtonClick(object? sender, EventArgs e)
    {
        if (_encryptedTextBox.TextLength == 0)
        {
            SetError("没有可复制的密文。");
            return;
        }

        Clipboard.SetText(_encryptedTextBox.Text);
        SetSuccess("密文已复制到剪贴板。");
    }

    private void ClearButtonClick(object? sender, EventArgs e)
    {
        _plainTextBox.Clear();
        _encryptedTextBox.Clear();
        _statusLabel.ForeColor = Color.DimGray;
        _statusLabel.Text = "已清空连接串，AES 密钥保持不变。";
    }

    private byte[] ReadKey()
    {
        var keyText = _keyTextBox.Text.Trim();
        if (keyText.Length == 0)
        {
            throw new InvalidOperationException("请输入 AES 密钥。");
        }

        byte[] key;

        try
        {
            key = Convert.FromBase64String(keyText);
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException("AES 密钥不是有效的 Base64 内容。", exception);
        }

        if (key.Length != KeySize)
        {
            throw new InvalidOperationException("AES 密钥解码后必须正好为 32 字节。");
        }

        return key;
    }

    private static string Encrypt(string connectionString, byte[] key)
    {
        var plainText = Encoding.UTF8.GetBytes(connectionString);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var cipherText = new byte[plainText.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plainText, cipherText, tag, AdditionalData);

        var payload = new byte[NonceSize + TagSize + cipherText.Length];
        Buffer.BlockCopy(nonce, 0, payload, 0, nonce.Length);
        Buffer.BlockCopy(tag, 0, payload, NonceSize, tag.Length);
        Buffer.BlockCopy(cipherText, 0, payload, NonceSize + TagSize, cipherText.Length);

        return EncryptedPrefix + Convert.ToBase64String(payload);
    }

    private static string Decrypt(string encryptedValue, byte[] key)
    {
        if (!encryptedValue.StartsWith(EncryptedPrefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("密文必须以 enc:v1: 开头。");
        }

        byte[] payload;

        try
        {
            payload = Convert.FromBase64String(encryptedValue[EncryptedPrefix.Length..]);
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException("密文不是有效的 Base64 内容。", exception);
        }

        if (payload.Length <= NonceSize + TagSize)
        {
            throw new InvalidOperationException("密文格式无效。");
        }

        var cipherTextSize = payload.Length - NonceSize - TagSize;
        var nonce = new byte[NonceSize];
        var tag = new byte[TagSize];
        var cipherText = new byte[cipherTextSize];
        var plainText = new byte[cipherTextSize];

        Buffer.BlockCopy(payload, 0, nonce, 0, NonceSize);
        Buffer.BlockCopy(payload, NonceSize, tag, 0, TagSize);
        Buffer.BlockCopy(payload, NonceSize + TagSize, cipherText, 0, cipherTextSize);

        try
        {
            using var aes = new AesGcm(key, TagSize);
            aes.Decrypt(nonce, cipherText, tag, plainText, AdditionalData);
        }
        catch (CryptographicException exception)
        {
            throw new InvalidOperationException("解密失败，请检查密钥和密文是否匹配。", exception);
        }

        return Encoding.UTF8.GetString(plainText);
    }

    private void RunSafely(Action action)
    {
        try
        {
            action();
        }
        catch (Exception exception)
        {
            SetError(exception.Message);
        }
    }

    private void SetSuccess(string message)
    {
        _statusLabel.ForeColor = Color.DarkGreen;
        _statusLabel.Text = message;
    }

    private void SetError(string message)
    {
        _statusLabel.ForeColor = Color.Firebrick;
        _statusLabel.Text = message;
    }

    private static Label CreateLabel(string text)
    {
        return new Label
        {
            AutoSize = true,
            Margin = new Padding(0, 12, 0, 6),
            Text = text
        };
    }

    private static void ConfigureMultilineTextBox(TextBox textBox)
    {
        textBox.AcceptsReturn = true;
        textBox.Dock = DockStyle.Fill;
        textBox.Multiline = true;
        textBox.ScrollBars = ScrollBars.Vertical;
        textBox.WordWrap = true;
    }

    private static Button CreateButton(string text, EventHandler clickHandler)
    {
        var button = new Button
        {
            AutoSize = true,
            Padding = new Padding(14, 5, 14, 5),
            Text = text
        };
        button.Click += clickHandler;
        return button;
    }

    private static FlowLayoutPanel CreateButtonPanel(params Button[] buttons)
    {
        var panel = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(0, 10, 0, 0)
        };
        panel.Controls.AddRange(buttons);
        return panel;
    }
}
