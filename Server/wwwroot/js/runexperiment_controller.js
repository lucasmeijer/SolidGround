import { Controller } from "@hotwired/stimulus"

export default class RunExperimentController extends Controller {
    connect() {
        this.element.addEventListener("turbo:submit-start", this.handleSubmit.bind(this))
        this.element.addEventListener("turbo:submit-end", this.handleSubmitEnd.bind(this))
    }

    disconnect() {
        this.element.removeEventListener("turbo:submit-start", this.handleSubmit.bind(this))
        this.element.removeEventListener("turbo:submit-end", this.handleSubmitEnd.bind(this))
    }
    
    handleSubmitEnd(event) {
        // Find the closest dialog element and close it
        const dialog = this.element.closest('dialog')
        if (dialog) {
            dialog.close()
        }
    }
    
    handleSubmit(event) {
        event.preventDefault()

        const formData = new FormData(this.element)
        const data = {
            'string_variables': [],
            'inputs': window.appSnapshot.inputs,
            'baseurl': formData.get('baseurl')
        }
       
        for (const [key, value] of formData.entries()) {
            if (key.startsWith('SolidGroundVariable_')) {
                data['string_variables'].push({
                    name: key.substring('SolidGroundVariable_'.length),
                    value: value
                });
            }
        }
        
        event.detail.formSubmission.fetchRequest.headers["Content-Type"] = "application/json"
        event.detail.formSubmission.fetchRequest.body = JSON.stringify(data)
    }
}
