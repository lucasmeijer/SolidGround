import {Controller} from "@hotwired/stimulus";

export default class FilterBar extends Controller {
    static targets = [ "searchbar" ]
    static values = {
        tags: Array,
        alltags: Array,
    }
    
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
                    
        var body = JSON.stringify({
            tags: newtags ?? this.tagsValue,
            tags_changed: newtags != null,
            search: this.searchbarTarget.value
        });
        
        const response = await fetch('/api/search', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Accept': 'text/vnd.turbo-stream.html',
            },
            body: body
        });

        if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
        Turbo.renderStreamMessage(await response.text());
    }
}