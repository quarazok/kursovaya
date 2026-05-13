// Автоматически добавляет Bearer-токен ко всем запросам к нашему API
const _originalFetch = window.fetch.bind(window);
window.fetch = async function (url, options = {}) {
  const token = localStorage.getItem('token');
  if (token && String(url).includes('localhost:5206')) {
    options.headers = { 'Authorization': `Bearer ${token}`, ...options.headers };
  }
  const response = await _originalFetch(url, options);
  if (response.status === 401 && !String(url).includes('/auth/')) {
    localStorage.clear();
    window.location.href = 'login.html';
  }
  return response;
};

function requireAuth() {
  if (!localStorage.getItem('token')) {
    window.location.href = 'login.html';
    return;
  }
  // Клиентам вход в CRM запрещён
  if (localStorage.getItem('role') === 'Client') {
    window.location.href = '../client/dashboard.html';
  }
}

function logout() {
  localStorage.clear();
  window.location.href = 'login.html';
}

function setupHeader() {
  const role      = localStorage.getItem('role');
  const firstName = localStorage.getItem('firstName') ?? '';
  const lastName  = localStorage.getItem('lastName') ?? '';
  const headerInner = document.querySelector('.header-inner');
  if (!headerInner) return;

  // Ссылка «Пользователи» для админа
  if (role === 'Admin') {
    const nav = headerInner.querySelector('nav');
    if (nav) {
      const a = document.createElement('a');
      a.href = 'users.html';
      a.textContent = 'Пользователи';
      if (window.location.pathname.replace(/\\/g,'/').endsWith('users.html')) {
        a.classList.add('active');
      }
      nav.appendChild(a);
    }
  }

  // Имя пользователя + кнопка выхода
  const userDiv = document.createElement('div');
  userDiv.style.cssText = 'display:flex;align-items:center;gap:10px;margin-left:auto;white-space:nowrap;';
  userDiv.innerHTML = `
    <span style="font-size:13px;color:var(--text-muted)">👤 ${firstName} ${lastName}</span>
    <button class="btn btn-secondary btn-sm" onclick="logout()">Выйти</button>
  `;
  headerInner.appendChild(userDiv);
}
