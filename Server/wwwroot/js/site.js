import { Application } from "@hotwired/stimulus";
import TagsController from "./tags.js";
// noinspection ES6UnusedImports
import * as Turbo from "@hotwired/turbo"
import FilterBar from "./filterbar.js";
import CursorController from "./cursor_controller.js";

// Initialize Stimulus application
const application = Application.start();

// Register your controller
application.register("tags", TagsController);
application.register("filterbar", FilterBar);
//application.register("cursor", CursorController);
