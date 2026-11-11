(() => {
    'use strict';
    const form = document.querySelector('[data-outgoing-form]');
    if (!form) return;
    const product = form.querySelector('#ProductoTerminadoId');
    const client = form.querySelector('#ClientePuntoVentaId');
    const quantity = form.querySelector('#Cantidad');
    const preview = form.querySelector('[data-outgoing-preview]');
    const available = form.querySelector('[data-outgoing-stock]');
    const recipientLink = form.querySelector('[data-new-recipient]');
    const number = new Intl.NumberFormat('es-MX', { maximumFractionDigits: 3 });
    function update() {
        const option = product.selectedOptions[0];
        const stock = Number(option?.dataset.stock);
        const amount = quantity.valueAsNumber;
        const unit = option?.dataset.unit || '';
        available.textContent = product.value ? `${number.format(stock)} ${unit} disponibles al abrir esta pantalla. Se comprobarán otra vez al guardar.` : 'La disponibilidad se comprobará nuevamente al guardar.';
        const url = new URL(recipientLink.href);
        if (product.value) url.searchParams.set('productoId', product.value); else url.searchParams.delete('productoId');
        recipientLink.href = url.href;
        if (!product.value || !client.value || !Number.isFinite(amount) || amount <= 0) {
            preview.textContent = 'Selecciona el producto, el destinatario y la cantidad para revisar la salida.';
        } else if (amount > stock) {
            preview.textContent = `La cantidad supera las existencias mostradas (${number.format(stock)} ${unit}). Revisa la cantidad antes de registrar.`;
        } else {
            const remaining = Math.round((stock - amount) * 1000) / 1000;
            preview.textContent = `Entregarás ${number.format(amount)} ${unit} de ${option.dataset.name} a ${client.selectedOptions[0].textContent.trim()}. Quedarían ${number.format(remaining)} ${unit}, sujeto a la disponibilidad al confirmar.`;
        }
    }
    form.addEventListener('input', update);
    form.addEventListener('change', update);
    update();
})();
