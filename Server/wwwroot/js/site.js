import { Application } from "@hotwired/stimulus";
import TagsController from "./tags.js";
// noinspection ES6UnusedImports
import * as Turbo from "@hotwired/turbo"
import FilterBar from "./filterbar.js";
import CursorController from "./cursor_controller.js";
import FormToJsonController from "./formtojson_controller.js";
import AutoSubmitSelectController from "./autosubmit_controller.js";
import RunExperimentController from "./runexperiment_controller.js";
import TextAreaResizeController from "./textarearesize_controller.js";
import AutoReloadController from "./autoreload_controller.js";
import RestoreCursorPositionController from "./RestoreCursorPositionController.js";
import PromptController from "./prompt_controller.js";

// Initialize Stimulus application
const application = Application.start();

// Register your controller
application.register("tags", TagsController);
application.register("filterbar", FilterBar);
application.register("formtojson", FormToJsonController);
application.register("autosubmit", AutoSubmitSelectController);
application.register("runexperiment", RunExperimentController);
application.register("textarearesize", TextAreaResizeController);
application.register("autoreload", AutoReloadController);
application.register("restorecursorposition", RestoreCursorPositionController);
application.register("prompt", PromptController);

//application.register("cursor", CursorController);

// function getSolidGroundTabId() {
//     const tabId = localStorage.getItem('tabId') || crypto.randomUUID();
//     localStorage.setItem('tabId', tabId);
//     return tabId;
// }

export function addHeadersToFetchOptions(fetchOptions)
{
    //fetchOptions.headers['X-Tab-Id'] = getSolidGroundTabId();
    fetchOptions.headers['X-App-State'] = JSON.stringify(window.appSnapshot.state);
}

document.addEventListener('turbo:before-fetch-request', (event) => {
    addHeadersToFetchOptions(event.detail.fetchOptions);
});
