const _originalFetch = window.fetch.bind(window);
window.fetch = async function (url, options = {}) {
  const token = localStorage.getItem('client_token');
  if (token && String(url).includes('localhost:5206')) {
    options.headers = { 'Authorization': `Bearer ${token}`, ...options.headers };
  }
  const response = await _originalFetch(url, options);
  if (response.status === 401 && !String(url).includes('/auth/') && !String(url).includes('/register')) {
    localStorage.removeItem('client_token');
    localStorage.removeItem('client_role');
    localStorage.removeItem('client_firstName');
    localStorage.removeItem('client_lastName');
    window.location.href = 'login.html';
  }
  return response;
};

function requireClientAuth() {
  if (!localStorage.getItem('client_token')) {
    window.location.href = 'login.html';
  }
}

function clientLogout() {
  localStorage.removeItem('client_token');
  localStorage.removeItem('client_role');
  localStorage.removeItem('client_firstName');
  localStorage.removeItem('client_lastName');
  window.location.href = 'index.html';
}

function isClientLoggedIn() {
  return !!localStorage.getItem('client_token');
}

function setupClientHeader() {
  const firstName = localStorage.getItem('client_firstName') ?? '';
  const lastName  = localStorage.getItem('client_lastName') ?? '';
  const headerInner = document.querySelector('.client-header-inner');
  if (!headerInner) return;

  const userDiv = document.createElement('div');
  userDiv.style.cssText = 'display:flex;align-items:center;gap:10px;margin-left:auto;white-space:nowrap;';
  userDiv.innerHTML = `
    <span style="font-size:13px;color:var(--text-muted)">👤 ${firstName} ${lastName}</span>
    <button class="c-btn c-btn-outline" onclick="clientLogout()">Выйти</button>
  `;
  headerInner.appendChild(userDiv);
}
