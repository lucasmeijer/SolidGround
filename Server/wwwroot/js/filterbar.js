import {Controller} from "@hotwired/stimulus";
import {addHeadersToFetchOptions} from "./site.js";

export default class FilterBar extends Controller {
    static targets = [ "searchbar" ]
    static values = {
        tags: Array,
        alltags: Array,
    }

    // connect() {
    //     // Store the bound function as an instance property
    //     this.boundHandleFrameRender = (event) => this.handleFrameRender(event)
    //     this.element.addEventListener("turbo:frame-render", this.boundHandleFrameRender)
    // }
    //
    // async handleFrameRender(event) {
    //     await this.sendFiltersToServer();
    // }
    //
    // disconnect() {
    //     // Remove the event listener using the stored reference
    //     this.element.removeEventListener("turbo:frame-render", this.boundHandleFrameRender)
    // }
    async clearSearchBar() {
        this.searchbarTarget.value = "";
        await this.sendFiltersToServer();
    }
    
    async removeTag(event) {
        let tagId = event.params.tagid;
        await this.sendFiltersToServer(this.tagsValue.filter(id => id !== tagId));
    }

    async addTagDropdownChanged(event) {
        await this.sendFiltersToServer([...this.tagsValue, parseInt(event.target.value)]);
    }
    
    async sendFiltersToServer(newtags) {
        if (newtags instanceof Event)
        {
            newtags = null
        }

        const executions = Array.from(this.element.querySelectorAll(".execution_checkbox"))
            .filter(checkbox => checkbox.checked)
            .map(checkbox => parseInt(checkbox.value));
                    
        var body = JSON.stringify({
            tags: newtags ?? this.tagsValue,
            tags_changed: newtags != null,
            search: this.searchbarTarget.value,
            executions: executions
        });
        
        let options = {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Accept': 'text/vnd.turbo-stream.html',
            },
            body: body
        };
        addHeadersToFetchOptions(options);
        const response = await fetch('/api/search', options);

        if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
        Turbo.renderStreamMessage(await response.text());
    }
}