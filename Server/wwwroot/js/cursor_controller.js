// app/javascript/controllers/cursor_controller.js

import { Controller } from "@hotwired/stimulus";

export default class CursorController extends Controller {
    static targets = ["input"];

    connect() {
        this.saveCursorPosition = this.saveCursorPosition.bind(this);
        this.restoreCursorPosition = this.restoreCursorPosition.bind(this);

        // Listen for Turbo events on the frame
        this.frame = this.element.closest("turbo-frame");
        this.frame.addEventListener("turbo:before-fetch-request", this.saveCursorPosition);
        this.frame.addEventListener("turbo:frame-render", this.restoreCursorPosition);
    }

    disconnect() {
        this.frame.removeEventListener("turbo:before-fetch-request", this.saveCursorPosition);
        this.frame.removeEventListener("turbo:frame-render", this.restoreCursorPosition);
    }

    saveCursorPosition() {
        const input = this.inputTarget;
        this.cursorPosition = input.selectionStart;
    }

    restoreCursorPosition() {
        const input = this.inputTarget;
        input.setSelectionRange(this.cursorPosition, this.cursorPosition);
        input.focus();
    }
}
