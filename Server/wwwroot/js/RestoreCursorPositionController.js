// cursor_position_controller.js
import { Controller } from "@hotwired/stimulus"

export default class RestoreCursorPositionController extends Controller {
    connect() {
        this.saveCursorBound = this.saveCursor.bind(this)
        this.restoreCursorBound = this.restoreCursor.bind(this)

        document.addEventListener("turbo:before-visit", this.saveCursorBound)
        document.addEventListener("turbo:render", this.restoreCursorBound)
    }

    disconnect() {
        document.removeEventListener("turbo:before-visit", this.saveCursorBound)
        document.removeEventListener("turbo:render", this.restoreCursorBound)
    }

    saveCursor() {
        if (document.activeElement === this.element) {
            sessionStorage.setItem('cursorPosition', this.element.selectionStart)
            sessionStorage.setItem('wasInputFocused', 'true')
        }
    }

    restoreCursor() {
        if (sessionStorage.getItem('wasInputFocused') === 'true') {
            const position = parseInt(sessionStorage.getItem('cursorPosition'))
            this.element.focus()
            this.element.setSelectionRange(position, position)
            sessionStorage.removeItem('cursorPosition')
            sessionStorage.removeItem('wasInputFocused')
        }
    }
}
