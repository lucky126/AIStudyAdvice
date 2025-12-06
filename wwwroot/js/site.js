window.saveConfig = function (grade, subject, textbookVersion) {
  try {
    localStorage.setItem('grade', String(grade));
    localStorage.setItem('subject', subject || '');
    localStorage.setItem('textbookVersion', textbookVersion || '');
  } catch {}
};

window.loadConfig = function () {
  try {
    const g = parseInt(localStorage.getItem('grade') || '0');
    const s = localStorage.getItem('subject') || '';
    const t = localStorage.getItem('textbookVersion') || '';
    return { grade: isNaN(g) ? 0 : g, subject: s, textbookVersion: t };
  } catch {
    return { grade: 0, subject: '', textbookVersion: '' };
  }
};

