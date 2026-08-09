function openModal() {
    var modal = document.getElementById('productModal');
    if (modal) {
        modal.style.display = 'flex';
    }
}

function closeModal() {
    var modal = document.getElementById('productModal');
    if (modal) {
        modal.style.display = 'none';
    }
}
function trackMyOrder(orderId) {
    var area = document.getElementById('trackingArea');
    var trackId = document.getElementById('trackId');

    area.style.display = 'block';
    trackId.textContent = orderId;

    // Sab steps reset karo
    document.getElementById('step-placed').classList.remove('active');
    document.getElementById('step-shipped').classList.remove('active');
    document.getElementById('step-delivered').classList.remove('active');

    // Default — Placed active karo
    document.getElementById('step-placed').classList.add('active');
}