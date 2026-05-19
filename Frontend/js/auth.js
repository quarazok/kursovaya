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

// Какие роли допущены на каждую страницу CRM.
// Файл страницы → набор ролей сотрудников.
const PAGE_ACCESS = {
  'dashboard.html':  ['Admin', 'Operator', 'Courier', 'Accountant'],
  'clients.html':    ['Admin', 'Operator'],
  'couriers.html':   ['Admin', 'Operator'],
  'orders.html':     ['Admin', 'Operator', 'Courier', 'Accountant'],
  'deliveries.html': ['Admin', 'Operator', 'Courier'],
  'payments.html':   ['Admin', 'Operator', 'Accountant'],
  'users.html':      ['Admin'],
};

const ROLE_LABEL = {
  Admin:      'Админ',
  Operator:   'Оператор',
  Courier:    'Курьер',
  Accountant: 'Бухгалтер',
  Client:     'Клиент',
};

const STAFF_ROLES = ['Admin', 'Operator', 'Courier', 'Accountant'];

function currentRole() { return localStorage.getItem('role'); }
function hasRole(...roles) { return roles.includes(currentRole()); }

function requireAuth() {
  if (!localStorage.getItem('token')) {
    window.location.href = 'login.html';
    return;
  }
  const role = currentRole();

  // Клиентам вход в CRM запрещён
  if (role === 'Client') {
    window.location.href = '../client/dashboard.html';
    return;
  }

  // Неизвестная роль (например, устаревший токен со старой ролью Employee) — выкидываем на логин
  if (!STAFF_ROLES.includes(role)) {
    localStorage.clear();
    window.location.href = 'login.html';
    return;
  }

  // Гейтинг по странице
  const file = (window.location.pathname.split('/').pop() || 'dashboard.html').toLowerCase();
  const allowed = PAGE_ACCESS[file];
  if (allowed && !allowed.includes(role)) {
    // Не дашборд → отправляем на дашборд. С дашборда (file пустой/не в карте) никуда не редиректим.
    if (file !== 'dashboard.html') window.location.href = 'dashboard.html';
  }
}

function logout() {
  localStorage.clear();
  window.location.href = 'login.html';
}

function setupHeader() {
  const role      = currentRole();
  const firstName = localStorage.getItem('firstName') ?? '';
  const lastName  = localStorage.getItem('lastName') ?? '';
  const headerInner = document.querySelector('.header-inner');
  if (!headerInner) return;

  const nav = headerInner.querySelector('nav');

  // Скрываем пункты меню, которые недоступны текущей роли
  if (nav) {
    nav.querySelectorAll('a').forEach(a => {
      const href = (a.getAttribute('href') || '').toLowerCase();
      const allowed = PAGE_ACCESS[href];
      if (allowed && !allowed.includes(role)) a.style.display = 'none';
    });

    // Ссылка «Пользователи» для админа
    if (role === 'Admin') {
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
  const roleLabel = ROLE_LABEL[role] ?? role;
  const userDiv = document.createElement('div');
  userDiv.style.cssText = 'display:flex;align-items:center;gap:10px;margin-left:auto;white-space:nowrap;';
  userDiv.innerHTML = `
    <span style="font-size:13px;color:var(--text-muted)">👤 ${firstName} ${lastName} <span style="color:var(--orange);font-weight:600">· ${roleLabel}</span></span>
    <button class="btn btn-secondary btn-sm" onclick="logout()">Выйти</button>
  `;
  headerInner.appendChild(userDiv);
}
