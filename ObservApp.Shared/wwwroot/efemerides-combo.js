// Setup combo box click handlers to open popup on any click
window.setupComboBoxClickHandlers = function(ids) {
    if (!ids || !Array.isArray(ids)) return;

    ids.forEach(function(id) {
        try {
            var el = document.getElementById(id);
            if (!el) return;

            // Syncfusion EJ2 instances are stored on the root element in ej2_instances
            var inst = el.ej2_instances && el.ej2_instances[0];

            // Fallback: if id was assigned to inner input, try to find closest combobox wrapper
            var container = el.classList && el.classList.contains('e-combobox') ? el : el.closest('.e-combobox');
            if (!container) return;

            container.addEventListener('click', function(event) {
                // If clicking the dropdown icon, let default handler run
                if (event.target.closest('.e-input-group-icon') || event.target.closest('.e-dropdown-icon')) return;

                try {
                    if (inst && typeof inst.showPopup === 'function') {
                        inst.showPopup();
                    } else {
                        // As a fallback, try to trigger a focus+keydown ArrowDown on the input
                        var input = container.querySelector('input');
                        if (input) {
                            input.focus();
                            var e = new KeyboardEvent('keydown', { key: 'ArrowDown', code: 'ArrowDown', bubbles: true });
                            input.dispatchEvent(e);
                        }
                    }
                } catch (ie) {
                    console.warn('Error opening combo popup for', id, ie);
                }
            }, false);
        } catch (e) {
            console.warn('Error setting up combo handler for', id, e);
        }
    });
};
