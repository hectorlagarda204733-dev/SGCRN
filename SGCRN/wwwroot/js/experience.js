(() => {
    'use strict';
    const safePath = value => {
        try {
            const url = new URL(value, location.origin);
            return url.origin === location.origin && (url.pathname === '/' || /^\/(Producto|MateriaPrima|EntradaInventario)(\/|$)/i.test(url.pathname))
                ? url.pathname + url.search : null;
        } catch { return null; }
    };
    // A per-tab trail, with ordinary server links as fallback for direct arrivals.
    const current = location.pathname + location.search;
    const back = document.querySelector('[data-return-link]');
    try {
        let trail = JSON.parse(sessionStorage.getItem('sgcrn.trail') || '[]');
        trail = Array.isArray(trail) ? trail.filter(x => typeof x === 'string' && safePath(x)) : [];
        const position = trail.lastIndexOf(current);
        if (position >= 0) trail = trail.slice(0, position + 1);
        else if (safePath(current)) trail.push(current);
        trail = trail.slice(-30);
        sessionStorage.setItem('sgcrn.trail', JSON.stringify(trail));
        // Never send a confirmation page back to the form that created the record.
        let previous = trail.slice(0, -1).reverse().find(x => !/\/(Create|Edit|Delete)(\/|\?|$)/i.test(x));
        if (back && previous) {
            back.href = previous;
            back.querySelector('span').textContent = 'Volver a la pantalla anterior';
        }
        document.querySelectorAll('.form-actions a').forEach(link => {
            if (back && link.textContent.trim() === 'Cancelar') link.href = back.href;
        });
        document.querySelectorAll('form[data-save-form]').forEach(form => {
            if (!['Producto', 'MateriaPrima'].includes(document.body.dataset.section)) return;
            const input = document.createElement('input');
            input.type = 'hidden'; input.name = 'returnUrl';
            input.value = safePath(back?.href) || '/' + document.body.dataset.section;
            form.append(input);
        });
    } catch { /* Navigation stays functional without session storage. */ }

    // Show progress only on an actual navigation. BFCache restores the resting state.
    document.addEventListener('click', event => {
        const link = event.target.closest('a[href]');
        if (!link || event.defaultPrevented || event.ctrlKey || event.metaKey || event.shiftKey || event.altKey || event.button !== 0 || link.target || link.hasAttribute('download')) return;
        if (safePath(link.href) && !link.hash && link.href !== location.href) document.body.classList.add('is-navigating');
    });
    document.addEventListener('submit', event => {
        if (!event.defaultPrevented) document.body.classList.add('is-navigating');
    });
    window.addEventListener('pageshow', () => document.body.classList.remove('is-navigating'));

    const editor = document.querySelector('[data-image-editor]');
    if (!editor) return;
    const fileInput = editor.querySelector('[type="file"]');
    const urlInput = editor.querySelector('[name="ImagenUrl"]');
    const remove = editor.querySelector('[name="QuitarImagen"][type="checkbox"]');
    const drop = editor.querySelector('[data-image-drop]');
    const preview = editor.querySelector('[data-image-preview]');
    const image = preview.querySelector('img');
    const caption = editor.querySelector('[data-image-caption]');
    const message = editor.querySelector('[data-image-message]');
    const reset = editor.querySelector('[data-image-reset]');
    let objectUrl;
    function show(src, text) {
        if (objectUrl) { URL.revokeObjectURL(objectUrl); objectUrl = null; }
        preview.hidden = !src;
        if (src) image.src = src; else image.removeAttribute('src');
        caption.textContent = text;
    }
    function restore() {
        fileInput.value = ''; urlInput.value = ''; fileInput.setCustomValidity('');
        if (remove) remove.checked = false;
        reset.hidden = true; message.textContent = '';
        show(editor.dataset.currentImage, 'Imagen actual');
    }
    function select(file) {
        if (!file) { restore(); return; }
        if (!['image/jpeg', 'image/png', 'image/webp'].includes(file.type) || file.size > 2097152 || !file.size) {
            fileInput.value = '';
            message.textContent = 'Selecciona una imagen JPG, PNG o WebP de hasta 2 MB.';
            fileInput.setCustomValidity(message.textContent);
            return;
        }
        const transfer = new DataTransfer(); transfer.items.add(file); fileInput.files = transfer.files;
        fileInput.setCustomValidity(''); urlInput.value = '';
        if (remove) remove.checked = false;
        show('', ''); objectUrl = URL.createObjectURL(file);
        image.src = objectUrl; preview.hidden = false; reset.hidden = false;
        caption.textContent = file.name + ' · ' + Math.ceil(file.size / 1024) + ' KB';
        message.textContent = 'Imagen seleccionada. Se guardará junto con el producto.';
    }
    fileInput.addEventListener('change', () => select(fileInput.files[0]));
    drop.addEventListener('dragover', event => { event.preventDefault(); drop.classList.add('is-dragging'); });
    drop.addEventListener('dragleave', event => { if (!drop.contains(event.relatedTarget)) drop.classList.remove('is-dragging'); });
    drop.addEventListener('drop', event => {
        event.preventDefault(); drop.classList.remove('is-dragging');
        if (event.dataTransfer.files.length !== 1) { message.textContent = 'Adjunta una sola imagen por producto.'; return; }
        select(event.dataTransfer.files[0]);
    });
    drop.addEventListener('paste', event => {
        const file = [...event.clipboardData.items].find(x => x.kind === 'file')?.getAsFile();
        if (file) { event.preventDefault(); select(file); }
    });
    image.addEventListener('error', () => {
        preview.hidden = true;
        message.textContent = 'No se pudo mostrar esta imagen. Comprueba el archivo o el enlace.';
        if (fileInput.files.length) fileInput.setCustomValidity('Selecciona una imagen válida.');
    });
    reset.addEventListener('click', restore);
    remove?.addEventListener('change', () => {
        if (remove.checked) {
            fileInput.value = ''; urlInput.value = ''; fileInput.setCustomValidity(''); show('', ''); reset.hidden = false;
            message.textContent = 'La imagen se quitará al guardar los cambios.';
        } else restore();
    });
    urlInput.addEventListener('input', () => {
        fileInput.value = ''; fileInput.setCustomValidity('');
        if (remove) remove.checked = false;
        const url = urlInput.value.trim(); reset.hidden = !url;
        if (/^https?:\/\//i.test(url) && urlInput.validity.valid) show(url, 'Imagen desde un enlace');
        else show(url ? '' : editor.dataset.currentImage, 'Imagen actual');
        message.textContent = '';
    });
    image.referrerPolicy = 'no-referrer';
    show(editor.dataset.currentImage, 'Imagen actual');
    if (remove?.checked) remove.dispatchEvent(new Event('change'));
    if (urlInput.value) {
        editor.querySelector('details').open = true;
        urlInput.dispatchEvent(new Event('input'));
    }
})();
