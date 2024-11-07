import {Controller} from "@hotwired/stimulus";
import {addHeadersToFetchOptions} from "./site.js";
import * as Turbo from "@hotwired/turbo"

export default class FilterBar extends Controller {
    static targets = [ "searchbar" ]
    static values = {
        tags: Array,
        alltags: Array,
    }
    
    async clearSearchBar() {
        window.appSnapshot.state.search = "";
        this.searchbarTarget.value = "";
        await this.sendFiltersToServer();
    }
    
    async removeTag(event) {
        let tagId = event.params.tagid;
        
        window.appSnapshot.state.tags = window.appSnapshot.state.tags.filter(id => id !== tagId);
        Turbo.visit(window.location.href)
        // await this.sendFiltersToServer(this.tagsValue.filter(id => id !== tagId));
    }

    async addTagDropdownChanged(event) {
        window.appSnapshot.state.tags = [...window.appSnapshot.state.tags, parseInt(event.target.value)]
        Turbo.visit(window.location.href)
        //await this.sendFiltersToServer([...this.tagsValue, parseInt(event.target.value)]);
    }
    
    async executionCheckboxClicked(event) {
        window.appSnapshot.state.executions = Array.from(this.element.querySelectorAll(".execution_checkbox"))
            .filter(checkbox => checkbox.checked)
            .map(checkbox => parseInt(checkbox.value));
        Turbo.visit(window.location.href)
    }
    
    async sendFiltersToServer() {
        let options = {headers:{}};
        addHeadersToFetchOptions(options);
        const response = await fetch('/api/search?query='+encodeURIComponent(this.searchbarTarget.value), options);
        if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
        Turbo.renderStreamMessage(await response.text());
    }
}