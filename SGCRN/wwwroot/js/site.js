(() => {
    'use strict';

    document.querySelectorAll('[data-dismiss-notice]').forEach(button => {
        button.hidden = false;
        button.addEventListener('click', () => {
            button.closest('.notice').remove();
            document.getElementById('contenido').focus();
        });
    });

    // Server-side validation remains authoritative; errors are announced and focused.
    const errors = document.querySelector('[data-form-errors]');
    if (errors) errors.focus();
    document.querySelectorAll('.input-validation-error').forEach(input => input.setAttribute('aria-invalid', 'true'));

    document.querySelectorAll('form[data-save-form]').forEach(form => {
        let submitting = false;
        form.addEventListener('submit', event => {
            if (submitting) { event.preventDefault(); return; }
            if (!form.reportValidity()) { event.preventDefault(); return; }
            submitting = true;
            form.setAttribute('aria-busy', 'true');
            const button = event.submitter || form.querySelector('[type="submit"]');
            if (button) {
                button.dataset.originalText = button.textContent;
                button.textContent = 'Guardando…';
                button.disabled = true;
                button.setAttribute('aria-busy', 'true');
            }
        });
        window.addEventListener('pageshow', () => {
            submitting = false;
            form.removeAttribute('aria-busy');
            form.querySelectorAll('[data-original-text]').forEach(button => {
                button.textContent = button.dataset.originalText;
                button.disabled = false;
                button.removeAttribute('aria-busy');
            });
        });
    });

    // Retain the user's catalog filters when returning from a detail or form.
    const section = document.querySelector('[data-catalog]');
    try {
        if (section) sessionStorage.setItem('sgcrn.catalog.' + section.dataset.catalog, location.pathname + location.search);
        document.querySelectorAll('[data-catalog-back]').forEach(link => {
            const saved = sessionStorage.getItem('sgcrn.catalog.' + link.dataset.catalogBack);
            if (!saved) return;
            const url = new URL(saved, location.origin);
            if (url.origin === location.origin && ['/' + link.dataset.catalogBack, '/' + link.dataset.catalogBack + '/Index', '/'].includes(url.pathname)) link.href = url.href;
        });
    } catch { /* Storage can be disabled; ordinary links remain usable. */ }

    document.querySelectorAll('[data-product-image]').forEach(image => {
        const hideBroken = () => { image.hidden = true; };
        image.addEventListener('error', hideBroken);
        if (image.complete && !image.naturalWidth) hideBroken();
    });

    const entryForm = document.querySelector('[data-entry-form]');
    if (!entryForm) return;
    const origins = entryForm.querySelectorAll('[name="TipoOrigen"]');
    const materia = entryForm.querySelector('#MateriaPrimaId');
    const producto = entryForm.querySelector('#ProductoTerminadoId');
    const quantity = entryForm.querySelector('#Cantidad');
    const preview = document.querySelector('[data-entry-preview]');
    const unit = document.querySelector('[data-quantity-unit]');
    const format = new Intl.NumberFormat('es-MX', { maximumFractionDigits: 3 });
    function update(clearOther) {
        const origin = entryForm.querySelector('[name="TipoOrigen"]:checked')?.value;
        const isPurchase = origin === 'Compra';
        entryForm.querySelector('[data-destination="Compra"]').hidden = origin !== 'Compra';
        entryForm.querySelector('[data-destination="Produccion"]').hidden = origin !== 'Produccion';
        materia.required = isPurchase;
        producto.required = origin === 'Produccion';
        if (clearOther) (isPurchase ? producto : materia).value = '';
        const selected = (isPurchase ? materia : producto).selectedOptions[0];
        const measure = selected?.dataset.unit;
        unit.textContent = measure ? 'Cantidad en ' + measure : 'Hasta 3 decimales.';
        const amount = Number(quantity.value);
        if (!origin || !selected?.value) {
            preview.textContent = 'Selecciona el origen y el artículo que recibiste.';
        } else if (!Number.isFinite(amount) || amount <= 0) {
            preview.textContent = 'Indica la cantidad recibida de ' + selected.dataset.name + '.';
        } else {
            preview.textContent = 'Se agregarán ' + format.format(amount) + ' ' + measure + ' a las existencias de ' + selected.dataset.name + '.';
        }
    }
    origins.forEach(input => input.addEventListener('change', () => update(true)));
    [materia, producto, quantity].forEach(input => input.addEventListener('input', () => update(false)));
    update(false);
})();
