import { Controller } from "@hotwired/stimulus"

export default class FormToJsonController extends Controller {
    static values = {
        url: String
    }
    static targets = ["errorMessage"]

    async submit(event) {
        event.preventDefault()

        // Collect form data
        const formData = new FormData(this.element)
        const data = {}

        for (const [key, value] of formData.entries()) {
            // Handle multiple select inputs
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

        // Simple validation example: check required fields
        const requiredFields = this.element.querySelectorAll('[required]')
        for (const field of requiredFields) {
            if (!formData.get(field.name)) {
                this.errorMessageTarget.textContent = `${field.name} is required.`
                return
            }
        }

        // Clear previous error messages
        this.errorMessageTarget.textContent = ''

        const token = document.querySelector('meta[name="csrf-token"]')?.getAttribute('content')

        try {
            const response = await fetch(this.urlValue, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Accept': 'text/vnd.turbo-stream.html',
                    ...(token && { 'X-CSRF-Token': token })
                },
                body: JSON.stringify(data)
            })

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`)
            }

            const html = await response.text()
            Turbo.renderStreamMessage(html)
            this.element.reset() // Reset the form
        } catch (error) {
            console.error('Fetch error:', error)
            this.errorMessageTarget.textContent = 'An error occurred while submitting the form.'
        }
    }
}
