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
function trackMyOrder(orderId, status) {
    var area = document.getElementById('trackingArea');
    var trackId = document.getElementById('trackId');

    area.style.display = 'block';
    trackId.textContent = '#' + orderId;

    var cancelledMsg = document.getElementById('cancelledMessage');
    var normalStepper = document.getElementById('normalStepper');

    if (status === 'Cancelled') {
        if (cancelledMsg) cancelledMsg.style.display = 'block';
        if (normalStepper) normalStepper.style.display = 'none';
    } else {
        if (cancelledMsg) cancelledMsg.style.display = 'none';
        if (normalStepper) normalStepper.style.display = 'flex';

        document.getElementById('step-placed').classList.remove('active');
        document.getElementById('step-shipped').classList.remove('active');
        document.getElementById('step-delivered').classList.remove('active');
        document.getElementById('step-placed').classList.add('active');
    }

    area.scrollIntoView({ behavior: 'smooth' });
}