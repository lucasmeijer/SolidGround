import { Controller } from "@hotwired/stimulus"

export default class FormToJsonController extends Controller {

    static targets = ["errorMessage"]

    connect() {
        this.element.addEventListener("turbo:submit-start", this.handleSubmit.bind(this))
    }

    disconnect() {
        this.element.removeEventListener("turbo:submit-start", this.handleSubmit.bind(this))
    }

    handleSubmit(event) {
        // Prevent the default Turbo submission
        event.preventDefault()

        // Collect form data and transform to JSON
        const formData = new FormData(this.element)
        const data = {}

        for (const [key, value] of formData.entries()) {
            if (data[key]) {
                if (Array.isArray(data[key])) {
                    data[key].push(value)
                } else {
                    data[key] = [data[key], value]
                }
            } else {
                data[key] = value
            }
        }

        // Validate required fields
        const requiredFields = this.element.querySelectorAll('[required]')
        for (const field of requiredFields) {
            if (!formData.get(field.name)) {
                this.errorMessageTarget.textContent = `${field.name} is required.`
                return
            }
        }

        // Clear previous error messages
        this.errorMessageTarget.textContent = ''
        
        // Use Turbo's fetch request
        event.detail.formSubmission.fetchRequest.headers["Content-Type"] = "application/json"
        event.detail.formSubmission.fetchRequest.body = JSON.stringify(data)
    }
}
