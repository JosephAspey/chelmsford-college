// Small enhancements only: every page works without JavaScript.
document.documentElement.classList.remove('no-js');

// Mobile menu
const toggle = document.querySelector('[data-menu-toggle]');
const nav = document.getElementById('primary-nav');
if (toggle && nav) {
  toggle.addEventListener('click', () => {
    const open = toggle.getAttribute('aria-expanded') === 'true';
    toggle.setAttribute('aria-expanded', String(!open));
    nav.classList.toggle('is-open', !open);
  });
  document.addEventListener('keydown', e => {
    if (e.key === 'Escape' && toggle.getAttribute('aria-expanded') === 'true') {
      toggle.setAttribute('aria-expanded', 'false');
      nav.classList.remove('is-open');
      toggle.focus();
    }
  });
}

// Course filters apply as soon as a box is ticked; the Apply button stays for keyboard and no-JS users
document.querySelectorAll('[data-auto-submit]').forEach(form => {
  form.addEventListener('change', e => {
    if (e.target.matches('input[type="checkbox"]')) form.requestSubmit();
  });
});

// Open or close every section of a course page at once
document.querySelectorAll('[data-accordion-toggle]').forEach(button => {
  const sections = document.querySelectorAll(button.dataset.accordionToggle + ' details');
  const update = () => {
    const allOpen = [...sections].every(d => d.open);
    button.textContent = allOpen ? 'Close all sections' : 'Open all sections';
    button.setAttribute('aria-expanded', String(allOpen));
  };
  button.addEventListener('click', () => {
    const open = ![...sections].every(d => d.open);
    sections.forEach(d => { d.open = open; });
    update();
  });
  sections.forEach(d => d.addEventListener('toggle', update));
  update();
});

// Video: load the YouTube player only when someone presses play, so no cookies are set before then
document.querySelectorAll('[data-video-id]').forEach(button => {
  button.addEventListener('click', () => {
    const frame = document.createElement('iframe');
    frame.src = `https://www.youtube-nocookie.com/embed/${encodeURIComponent(button.dataset.videoId)}?autoplay=1&rel=0`;
    frame.title = button.dataset.videoTitle || 'Video';
    frame.allow = 'autoplay; encrypted-media; picture-in-picture';
    frame.allowFullscreen = true;
    button.closest('.video__frame').replaceChildren(frame);
    frame.focus();
  });
});

// Card number spacing on the demo provider page
document.querySelectorAll('[data-card-number]').forEach(input => {
  input.addEventListener('input', () => {
    const digits = input.value.replace(/\D/g, '').slice(0, 19);
    input.value = digits.replace(/(.{4})/g, '$1 ').trim();
  });
});
