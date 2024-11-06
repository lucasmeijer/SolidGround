import { Controller } from "@hotwired/stimulus"

export default class AutoReloadController extends Controller {
    connect() {
        this.interval = setInterval(() => {
            
            //this seems to work to trigger a reload.
            this.element.src = this.element.getAttribute('data-src');
        }, 5000);
    }

    disconnect() {
        if (this.interval) {
            clearInterval(this.interval);
        }
    }
}
