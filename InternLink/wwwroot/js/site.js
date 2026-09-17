// Theme handling, ported from the original static InternLink page.
// Pages now come from the server, so instead of toggling `.section` visibility
// we simply persist the light/dark choice across full-page navigations.

function initTheme() {
    const savedTheme = localStorage.getItem('internlink-theme') || 'dark';
    document.body.classList.remove('dark-theme', 'light-theme');
    document.body.classList.add(savedTheme + '-theme');
    updateThemeIcon();
}

function toggleTheme() {
    const isDark = document.body.classList.contains('dark-theme');
    const newTheme = isDark ? 'light' : 'dark';

    document.body.classList.remove('dark-theme', 'light-theme');
    document.body.classList.add(newTheme + '-theme');

    localStorage.setItem('internlink-theme', newTheme);
    updateThemeIcon();
}

function updateThemeIcon() {
    const isDark = document.body.classList.contains('dark-theme');
    const icon = document.getElementById('theme-icon');
    const themeName = document.getElementById('theme-name');

    if (icon) {
        icon.textContent = isDark ? '☀️' : '🌙';
    }
    if (themeName) {
        themeName.textContent = isDark ? 'Dark Mode' : 'Light Mode';
    }
}

document.addEventListener('DOMContentLoaded', initTheme);
