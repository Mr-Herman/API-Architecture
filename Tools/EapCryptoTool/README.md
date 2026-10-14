# EAP 连接串加密/解密工具

双击 `EapCryptoTool.exe` 后，手动填写：

1. AES Base64 密钥；
2. 明文连接串或 `enc:v1:` 密文；
3. 点击“加密”或“解密”。

工具不会读取 `.secrets`，不会保存密钥，也不会自动修改 `appsettings.json`。
