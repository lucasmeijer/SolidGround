import { Application } from "@hotwired/stimulus";
import TagsController from "./tags.js";
// noinspection ES6UnusedImports
import * as Turbo from "@hotwired/turbo"
import FilterBar from "./filterbar.js";
import CursorController from "./cursor_controller.js";
import FormToJsonController from "./formtojson_controller.js";
import AutoSubmitSelectController from "./autosubmit_controller.js";
import RunExperimentController from "./runexperiment_controller.js";

// Initialize Stimulus application
const application = Application.start();

// Register your controller
application.register("tags", TagsController);
application.register("filterbar", FilterBar);
application.register("formtojson", FormToJsonController);
application.register("autosubmit", AutoSubmitSelectController);
application.register("runexperiment", RunExperimentController);
//application.register("cursor", CursorController);

document.addEventListener("turbo:submit-start", (event) => {
    const form = event.target;

    // Only intercept if the attribute is present
    if (!form.hasAttribute('data-rewrite-form-to-json-body')) {
        return;  // Let Turbo handle it normally
    }

    event.preventDefault();

    // Convert form data to JSON
    const formData = new FormData(form);
    const jsonData = Object.fromEntries(formData.entries());

    // Create a new submission with JSON data
    const submission = new FormSubmission(form, {
        body: JSON.stringify(jsonData),
        contentType: 'application/json'
    });

    // Let Turbo handle it from here
    Turbo.navigator.submitForm(form, submission);
});