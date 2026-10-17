const translations = {
  'zh-CN': {
    title: 'EAP API 日志查看',
    subtitle: '只读查看本机 .log 文件 · 最新优先 · 不访问数据库',
    language: '语言',
    startDate: '开始日期',
    endDate: '结束日期',
    logType: '日志类型',
    eventType: '事件',
    all: '全部',
    keyword: '接口 / 关键字',
    keywordPlaceholder: 'TrackOut、错误内容……',
    tracePlaceholder: '精确追踪请求',
    search: '查询',
    time: '时间',
    endpoint: '接口',
    status: '状态',
    total: '总耗时',
    details: '详情',
    ready: '设置条件后查询',
    previous: '上一页',
    next: '下一页',
    logDetails: '日志详情',
    close: '关闭',
    loading: '正在读取日志……',
    noLogs: '该条件下没有日志。',
    loaded: '已加载 {count} 条日志。',
    page: '第 {page} 页 · 本页 {count} 条 · 每页 {pageSize} 条',
    truncated: ' · 已达安全扫描上限',
    noChain: '无方法调用链路',
    view: '查看',
    chain: '调用链路',
    failureLocation: '错误位置',
    rawLog: '原始日志',
    requestBody: '请求参数',
    responseBody: '返回正文',
    none: '无',
    queryFailed: '查询失败',
    invalidRange: '开始日期不能晚于结束日期。',
    futureDate: '不能选择未来日期。',
    rangeTooLong: '单次最多查询 {days} 天日志。'
  },
  'en-US': {
    title:'EAP API Log Viewer', subtitle:'Read-only local .log viewer - Newest first - No database access',
    language:'Language', startDate:'Start date', endDate:'End date', logType:'Log type', eventType:'Event', all:'All',
    keyword:'Endpoint / keyword', keywordPlaceholder:'TrackOut, error message...', tracePlaceholder:'Trace a request precisely',
    search:'Search', time:'Time', endpoint:'Endpoint', status:'Status', total:'Total', details:'Details',
    ready:'Set filters and search', previous:'Previous', next:'Next', logDetails:'Log details', close:'Close',
    loading:'Reading log files...', noLogs:'No logs match the filters.', loaded:'Loaded {count} log entries.',
    page:'Page {page} - {count} entries - {pageSize} per page', truncated:' - Safety scan limit reached',
    noChain:'No method call chain', view:'View', chain:'Call chain', failureLocation:'Failure location',
    rawLog:'Raw log', requestBody:'Request parameters', responseBody:'Response body', none:'None', queryFailed:'Query failed',
    invalidRange:'The start date cannot be later than the end date.',
    futureDate:'Future dates cannot be selected.', rangeTooLong:'A single query can cover at most {days} days.'
  }
};

const state = {
  page: 1,
  pageSize: 0,
  maxSearchDays: 0,
  hasNext: false,
  items: [],
  language: localStorage.getItem('eapLogViewerLanguage') || 'zh-CN'
};

const ids = ['language','startDate','endDate','level','keyword','traceId','search','notice','rows','pageInfo','previous','next','details','detailBody','closeDetails'];
const el = Object.fromEntries(ids.map(id => [id, document.getElementById(id)]));
const today = formatLocalDate(new Date());

el.startDate.value = today;
el.endDate.value = today;
el.startDate.max = today;
el.endDate.max = today;
el.language.value = state.language;
applyLanguage();
el.search.disabled = true;

el.language.addEventListener('change', () => {
  state.language = el.language.value;
  localStorage.setItem('eapLogViewerLanguage', state.language);
  applyLanguage();
  renderRows();
  validateDates();
});
el.startDate.addEventListener('change', () => {
  updateDateLimits('start');
  validateDates();
});
el.endDate.addEventListener('change', () => {
  updateDateLimits('end');
  validateDates();
});
el.search.addEventListener('click', () => loadPage(1));
el.previous.addEventListener('click', () => loadPage(Math.max(1, state.page - 1)));
el.next.addEventListener('click', () => state.hasNext && loadPage(state.page + 1));
el.closeDetails.addEventListener('click', () => el.details.close());
el.rows.addEventListener('click', event => {
  const button = event.target.closest('[data-index]');
  if (button) showDetails(state.items[Number(button.dataset.index)]);
});

initialize();

async function initialize() {
  try {
    const response = await fetch('/ops/logs/settings', {
      headers: { Accept:'application/json', 'Accept-Language':state.language }
    });
    const result = await response.json().catch(() => null);

    if (!response.ok || result?.Status !== 1) {
      throw new Error(result?.ReturnMessage || `${t('queryFailed')} (HTTP ${response.status})`);
    }

    state.pageSize = result.Data.PageSize;
    state.maxSearchDays = result.Data.MaxSearchDays;
    updateDateLimits();
    await loadPage(1);
  } catch (error) {
    showNotice(error.message, 'error');
  }
}

async function loadPage(page) {
  if (!validateDates()) return;

  const query = new URLSearchParams({
    StartDate: el.startDate.value,
    EndDate: el.endDate.value,
    Page: page
  });

  if (el.level.value) query.set('Level', el.level.value);
  if (el.keyword.value.trim()) query.set('Keyword', el.keyword.value.trim());
  if (el.traceId.value.trim()) query.set('TraceId', el.traceId.value.trim());

  el.search.disabled = true;
  showNotice(t('loading'), 'info');

  try {
    const response = await fetch(`/ops/logs/api?${query}`, {
      headers: { Accept:'application/json', 'Accept-Language':state.language }
    });
    const result = await response.json().catch(() => null);

    if (!response.ok || result?.Status !== 1) {
      throw new Error(result?.ReturnMessage || `${t('queryFailed')} (HTTP ${response.status})`);
    }

    const data = result.Data;
    state.page = data.Page;
    state.hasNext = data.HasNext;
    state.items = data.Items || [];
    renderRows();
    el.previous.disabled = !data.HasPrevious;
    el.next.disabled = !data.HasNext;
    el.pageInfo.textContent = format(t('page'), {
      page: data.Page,
      count: state.items.length,
      pageSize: data.PageSize
    })
      + (data.Truncated ? t('truncated') : '');
    showNotice(state.items.length ? format(t('loaded'), { count:state.items.length }) : t('noLogs'), 'info');
  } catch (error) {
    showNotice(error.message, 'error');
  } finally {
    el.search.disabled = false;
  }
}

function validateDates() {
  const start = parseDate(el.startDate.value);
  const end = parseDate(el.endDate.value);
  const current = parseDate(today);
  let message = '';

  if (start > end) {
    message = t('invalidRange');
  } else if (end > current) {
    message = t('futureDate');
  } else {
    const days = Math.floor((end - start) / 86400000) + 1;
    if (state.maxSearchDays > 0 && days > state.maxSearchDays) {
      message = format(t('rangeTooLong'), { days:state.maxSearchDays });
    }
  }

  el.search.disabled = Boolean(message);
  if (message) showNotice(message, 'error');
  else hideNotice();
  return !message;
}

function updateDateLimits(changedField) {
  if (state.maxSearchDays <= 0 || !el.startDate.value || !el.endDate.value) return;

  const rangeOffset = state.maxSearchDays - 1;
  const start = parseDate(el.startDate.value);
  const end = parseDate(el.endDate.value);

  if (changedField === 'start') {
    if (start > end) {
      el.endDate.value = el.startDate.value;
    } else {
      const latestEnd = addDays(start, rangeOffset);
      if (end > latestEnd) el.endDate.value = formatLocalDate(latestEnd);
    }
  } else if (changedField === 'end') {
    if (end < start) {
      el.startDate.value = el.endDate.value;
    } else {
      const earliestStart = addDays(end, -rangeOffset);
      if (start < earliestStart) el.startDate.value = formatLocalDate(earliestStart);
    }
  }
}

function addDays(date, days) {
  const result = new Date(date);
  result.setDate(result.getDate() + days);
  return result;
}

function renderRows() {
  if (!state.items.length) {
    el.rows.innerHTML = `<tr><td colspan="8" class="empty">${escapeHtml(t('noLogs'))}</td></tr>`;
    return;
  }

  el.rows.innerHTML = state.items.map((item, index) =>
    `<tr>
      <td>${escapeHtml(formatTime(item.Timestamp))}</td>
      <td><span class="level ${logLevelClass(item.Level)}">${escapeHtml(item.Level)}</span></td>
      <td>${escapeHtml(item.LogType)}</td>
      <td title="${escapeHtml([item.HttpMethod,item.Path].filter(Boolean).join(' '))}">${escapeHtml([item.HttpMethod,item.Path].filter(Boolean).join(' '))}</td>
      <td>${statusBadge(item.StatusCode)}</td>
      <td>${item.TotalMilliseconds == null ? '' : formatMs(item.TotalMilliseconds)}</td>
      <td>${escapeHtml(item.TraceId || '')}</td>
      <td><button class="details-button" data-index="${index}">${escapeHtml(t('view'))}</button></td>
    </tr>`
  ).join('');
}

function showDetails(item) {
  const steps = (item.TimingSteps || [])
    .map(step => `${step.Layer}[${step.Resource}].${step.Method}: ${formatMs(step.ElapsedMilliseconds)}`)
    .join('\n');

  const requestParameters = [
    item.QueryParameters ? `Query:\n${formatJson(item.QueryParameters)}` : '',
    item.RequestBody ? `Body:\n${formatJson(item.RequestBody)}` : ''
  ].filter(Boolean).join('\n');

  const responseBody = formatJson(item.ResponseBody);
  const payload = requestParameters || item.ResponseBody
    ? `<h3>${escapeHtml(t('requestBody'))}</h3><pre>${escapeHtml(requestParameters || t('none'))}</pre>
       <h3>${escapeHtml(t('responseBody'))}</h3><pre>${escapeHtml(responseBody || t('none'))}</pre>`
    : '';

  el.detailBody.innerHTML =
    `<div class="detail-grid">
      ${detailItem(t('time'), formatTime(item.Timestamp))}
      ${detailBadgeItem(t('logType'), item.Level, `level ${logLevelClass(item.Level)}`)}
      ${detailItem(t('eventType'), item.LogType)}
      ${detailBadgeItem('HTTP ' + t('status'), item.StatusCode ?? '', `status-code ${statusClass(item.StatusCode)}`)}
      ${detailItem(t('endpoint'), [item.HttpMethod,item.Path].filter(Boolean).join(' '))}
      ${detailItem(t('total'), item.TotalMilliseconds == null ? '' : formatMs(item.TotalMilliseconds))}
      ${item.ErrorCode == null ? detailItem('Code', '') : detailBadgeItem('Code', item.ErrorCode, `business-code ${businessCodeClass(item.ErrorCode)}`)}
      ${detailItem('TraceId', item.TraceId || '')}
    </div>
    <h3>${escapeHtml(t('chain'))}</h3>
    <pre>${escapeHtml(steps || t('noChain'))}</pre>
    <h3>${escapeHtml(t('failureLocation'))}</h3>
    <pre>${escapeHtml(item.FailureLocation || t('none'))}</pre>
    ${payload}
    <h3>${escapeHtml(t('rawLog'))}</h3>
    <pre>${escapeHtml(item.RawText)}</pre>`;

  el.details.showModal();
}

function applyLanguage() {
  document.documentElement.lang = state.language;
  document.title = t('title');
  document.querySelectorAll('[data-i18n]').forEach(node => node.textContent = t(node.dataset.i18n));
  document.querySelectorAll('[data-i18n-placeholder]').forEach(node => node.placeholder = t(node.dataset.i18nPlaceholder));
  el.pageInfo.textContent = format(t('page'), {
    page: state.page,
    count: state.items.length,
    pageSize: state.pageSize || '-'
  });
}

function t(key) { return translations[state.language]?.[key] || translations['zh-CN'][key] || key; }
function format(text, values) { return Object.entries(values).reduce((result, [key,value]) => result.replace(`{${key}}`, value), text); }
function detailItem(label, value) { return `<div class="detail-item"><strong>${escapeHtml(label)}</strong>${escapeHtml(String(value ?? ''))}</div>`; }
function detailBadgeItem(label, value, className) { return `<div class="detail-item"><strong>${escapeHtml(label)}</strong><span class="${className}">${escapeHtml(String(value ?? ''))}</span></div>`; }
function statusBadge(statusCode) { return statusCode == null ? '' : `<span class="status-code ${statusClass(statusCode)}">${escapeHtml(statusCode)}</span>`; }
function statusClass(statusCode) {
  const status = Number(statusCode);
  if (status >= 200 && status < 300) return 'status-success';
  if (status >= 300 && status < 400) return 'status-redirect';
  if (status >= 400 && status < 500) return 'status-client-error';
  if (status >= 500) return 'status-server-error';
  return 'status-other';
}
function logLevelClass(level) {
  const normalized = String(level || '').toUpperCase();
  return ['INFO','WARN','ERROR','FATAL'].includes(normalized) ? normalized : 'INFO';
}
function businessCodeClass(code) {
  const value = Number(code);
  if (value === 0 || value === 1 || value === 1000) return 'code-success';
  if (value >= 4000 && value < 5000) return 'code-client-error';
  if (value >= 5000) return 'code-server-error';
  return 'code-other';
}
function formatLocalDate(value) {
  const year = value.getFullYear();
  const month = String(value.getMonth() + 1).padStart(2, '0');
  const day = String(value.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}
function parseDate(value) { return new Date(`${value}T00:00:00Z`); }
function formatTime(value) { return value ? value.replace('T', ' ') : ''; }
function formatMs(value) { return `${Number(value).toFixed(1)} ms`; }
function formatJson(value) {
  if (!value) return '';

  try {
    return JSON.stringify(JSON.parse(value), null, 2);
  } catch {
    return value;
  }
}
function showNotice(message, type) { el.notice.textContent = message; el.notice.className = `notice show ${type}`; }
function hideNotice() { el.notice.textContent = ''; el.notice.className = 'notice'; }
function escapeHtml(value) { return String(value ?? '').replace(/[&<>'"]/g, char => ({'&':'&amp;','<':'&lt;','>':'&gt;',"'":'&#39;','"':'&quot;'}[char])); }
