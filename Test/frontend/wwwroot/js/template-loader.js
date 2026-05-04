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

            const scripts = Array.from(element.querySelectorAll('script'));
            for (const oldScript of scripts) {
                const newScript = document.createElement('script');
                if (oldScript.src) {
                    newScript.src = oldScript.src;
                } else {
                    newScript.textContent = oldScript.textContent;
                }
                document.body.appendChild(newScript);
                oldScript.remove();
            }
        }
    } catch (error) {
        console.error('Error loading component:', error);
    }
}

async function loadAllComponents() {
    if (typeof Auth !== 'undefined' && typeof Auth.ensureInitialized === 'function') {
        await Auth.ensureInitialized();
    }

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
    const currentPath = window.location.pathname.split('/').pop() || 'home.html';
    if (typeof AccessControl !== 'undefined' && AccessControl.pageRules) {
        const rule = AccessControl.pageRules[currentPath];
        if (rule !== undefined && !AccessControl.canAccess(rule)) {
            window.location.href = AccessControl.getDefaultRoute();
            return;
        }
    }
}

function initNavigation() {
    const currentPath = window.location.pathname.split('/').pop() || 'home.html';
    
    document.querySelectorAll('nav a').forEach(link => {
        const href = link.getAttribute('href');
        if (href === currentPath || (currentPath === '' && href === 'home.html')) {
            link.classList.add('bg-surface-container-high', 'text-primary-container');
            link.classList.remove('text-on-surface-variant');
        }
    });
}
