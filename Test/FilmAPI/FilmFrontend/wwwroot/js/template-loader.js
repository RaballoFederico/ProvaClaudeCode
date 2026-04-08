async function loadComponent(elementId, componentPath) {
    try {
        const response = await fetch(componentPath);
        if (!response.ok) {
            throw new Error(`Failed to load ${componentPath}`);
        }
        const html = await response.text();
        const element = document.getElementById(elementId);
        if (element) {
            element.innerHTML = html;
        }
    } catch (error) {
        console.error('Error loading component:', error);
    }
}

async function loadAllComponents() {
    const components = [
        { id: 'sidebar-container', path: '/components/sidebar.html' },
        { id: 'navbar-container', path: '/components/navbar.html' },
        { id: 'footer-container', path: '/components/footer.html' }
    ];

    await Promise.all(
        components.map(c => loadComponent(c.id, c.path))
    );

    initNavigation();
    applyPageAccessControl();
}

function getCurrentUser() {
    if (typeof Auth === 'undefined') return null;
    return Auth.getUser();
}

function isManagerUser() {
    if (typeof Auth === 'undefined' || !Auth.isAuthenticated()) return false;
    return Auth.canManageCatalog();
}

function isAuthenticatedUser() {
    return typeof Auth !== 'undefined' && Auth.isAuthenticated();
}

function applyPageAccessControl() {
    const currentPath = window.location.pathname.split('/').pop() || 'index.html';

    const managerOnlyPages = ['index.html', 'registi.html', 'proiezioni.html'];
    if (managerOnlyPages.includes(currentPath) && !isManagerUser()) {
        window.location.href = '/programmazione.html';
    }
}

function initNavigation() {
    const currentPath = window.location.pathname.split('/').pop() || 'index.html';
    
    document.querySelectorAll('nav a').forEach(link => {
        const href = link.getAttribute('href');
        if (href === currentPath || (currentPath === '' && href === 'index.html')) {
            link.classList.add('bg-[#2A2A2A]', 'text-[#E50914]');
            link.classList.remove('text-on-surface-variant');
        }
    });

    const menuToggle = document.getElementById('mobile-menu-toggle');
    const sidebar = document.getElementById('sidebar');
    
    if (menuToggle && sidebar) {
        menuToggle.addEventListener('click', () => {
            sidebar.classList.toggle('hidden');
            sidebar.classList.toggle('md:flex');
        });
    }

    const logoutBtn = document.getElementById('logout-btn');
    if (logoutBtn) {
        logoutBtn.addEventListener('click', (e) => {
            e.preventDefault();
            if (typeof Auth !== 'undefined' && Auth.isAuthenticated()) {
                Auth.logout();
            } else {
                window.location.href = '/login.html';
            }
        });
    }
}
