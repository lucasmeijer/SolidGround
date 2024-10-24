import { Controller } from "@hotwired/stimulus"

export default class AutoSubmitSelectController extends Controller {
    connect() {
        const selectElements = this.element.querySelectorAll('select')
        selectElements.forEach(select => {
            select.addEventListener('change', this.handleSelectChange.bind(this))
        })
    }

    handleSelectChange(event) {
        // Create and dispatch a submit event
        const submitEvent = new SubmitEvent('submit', {
            bubbles: true,
            cancelable: true
        })
        this.element.dispatchEvent(submitEvent)
    }

    disconnect() {
        const selectElements = this.element.querySelectorAll('select')
        selectElements.forEach(select => {
            select.removeEventListener('change', this.handleSelectChange.bind(this))
        })
    }
}