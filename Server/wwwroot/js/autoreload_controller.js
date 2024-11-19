import { Controller } from "@hotwired/stimulus"

export default class AutoReloadController extends Controller {
    connect() {
        this.interval = setInterval(() => {
            const frame = this.element.closest('turbo-frame');
            if (frame) {
                const src = frame.getAttribute('data-src');
                frame.src = src;
            }
        }, 3000);
    }

    disconnect() {
        if (this.interval) {
            clearInterval(this.interval);
        }
    }
}