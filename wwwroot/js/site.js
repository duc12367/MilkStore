window.showToast = function(message, type = 'success', duration = 3500) {
    let c = document.getElementById('toast-container');
    if (!c) { c = document.createElement('div'); c.id = 'toast-container'; document.body.appendChild(c); }
    const icons = { success: '✓', error: '✕', info: 'ℹ' };
    const t = document.createElement('div');
    t.className = `toast-msg ${type}`;
    t.innerHTML = `<span style="font-size:1.1rem">${icons[type]||'ℹ'}</span><span>${message}</span>`;
    c.appendChild(t);
    setTimeout(() => { t.style.animation = 'slideOut .3s ease forwards'; setTimeout(() => t.remove(), 300); }, duration);
};
document.addEventListener('DOMContentLoaded', function() {
    const te = document.getElementById('server-toast');
    if (te) showToast(te.dataset.message, te.dataset.type || 'success');

    const b = document.getElementById('back-to-top');
    if (b) {
        window.addEventListener('scroll', () => { b.style.opacity = window.scrollY > 300 ? '1' : '0'; });
        b.addEventListener('click', () => window.scrollTo({ top: 0, behavior: 'smooth' }));
    }
});
