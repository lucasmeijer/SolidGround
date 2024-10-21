import {Controller} from "@hotwired/stimulus";

export default class AutoSubmitController extends Controller {
    connect() {
        console.log("connect: "+this.element)
        let selectElement = this.element.querySelector("select");

        selectElement.addEventListener("change", () => {
            console.log("tags changed!");
            this.element.requestSubmit();
        })
    }
    disconnect() {
        console.log("disconnect: "+this.element)
    }
}