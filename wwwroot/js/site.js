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

window.renderMathJax = function () {
  if (window.MathJax) {
    // Console log for debugging
    console.log('Triggering MathJax render...');
    
    // Helper function to execute typeset
    const doRender = () => {
        if (window.MathJax.typesetPromise) {
            window.MathJax.typesetPromise().then(() => {
                console.log('MathJax typeset complete.');
            }).catch((err) => {
                console.log('MathJax typeset error: ' + err.message);
                // Retry once if error occurs
                setTimeout(() => {
                     window.MathJax.typesetPromise();
                }, 500);
            });
        } else {
            console.log('MathJax.typesetPromise not available.');
        }
    };

    // If MathJax is still starting up, wait for it
    if (window.MathJax.startup && window.MathJax.startup.promise) {
        window.MathJax.startup.promise.then(doRender);
    } else {
        doRender();
    }
  } else {
    console.log('MathJax not loaded yet, retrying in 100ms...');
    setTimeout(window.renderMathJax, 100);
  }
};

window.setMathContent = function (element, content) {
    if (!element) return;
    element.innerHTML = content;
    
    if (window.MathJax) {
        const doTypeset = () => {
            if (window.MathJax.typesetPromise) {
                window.MathJax.typesetPromise([element]).catch(err => console.log(err));
            }
        };

        if (window.MathJax.startup && window.MathJax.startup.promise) {
            window.MathJax.startup.promise.then(doTypeset);
        } else {
            doTypeset();
        }
    }
};
