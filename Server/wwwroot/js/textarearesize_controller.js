import { Controller } from "@hotwired/stimulus"

export default class TextAreaResizeController extends Controller {
    connect() {
        this.resize()
    }

    resize() {
        this.element.style.height = 'auto'
        this.element.style.height = this.element.scrollHeight + 'px'
    }
}