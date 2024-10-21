import { Application } from "@hotwired/stimulus";
import TagsController from "./tags.js";
// noinspection ES6UnusedImports
import * as Turbo from "@hotwired/turbo"

// Initialize Stimulus application
const application = Application.start();

// Register your controller
application.register("tags", TagsController);
