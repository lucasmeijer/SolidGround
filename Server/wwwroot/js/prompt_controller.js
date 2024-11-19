// prompt_controller.js
import { Controller } from "@hotwired/stimulus"

export default class PromptController extends Controller {
    static values = {
        outputId: String,
        path: String
    }

    async copy(event) {
        try {
            // Show loading state
            const originalText = this.element.textContent
            this.element.textContent = "Copying..."

            // Construct the URL by replacing {outputId} in the path
            const url = this.pathValue.replace('{outputId}', this.outputIdValue)

            // Make the API call
            const response = await fetch(url)

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`)
            }

            const text = await response.text()

            // Copy to clipboard
            await navigator.clipboard.writeText(text)

            // Show success state
            this.element.textContent = "Copied!"

        } catch (error) {
            console.error("Copy failed:", error)
            this.element.textContent = "Failed to copy"
        } finally {
            // Reset button text after 2 seconds
            setTimeout(() => {
                this.element.textContent = "Copy For Prompt"
            }, 2000)
        }
    }
}
