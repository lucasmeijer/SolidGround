import { Controller } from "@hotwired/stimulus"
export default class AutoReloadController extends Controller {
    connect() {
        this.interval = setInterval(() => {
            const details = this.element.querySelector('details');
            const wasOpen = details.open;

            this.element.reload();

            this.element.addEventListener('turbo:frame-load', () => {
                const newDetails = this.element.querySelector('details');
                newDetails.open = wasOpen;
            }, { once: true });
        }, 5000);
    }

    disconnect() {
        if (this.interval) {
            clearInterval(this.interval);
        }
    }
}
