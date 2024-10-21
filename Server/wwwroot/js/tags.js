import {Controller} from "@hotwired/stimulus";
import * as Turbo from "@hotwired/turbo"

export default class TagsController extends Controller {
    static values = {
        endpoint: String,
        tags: Array,
        alltags: Array,
    }
    connect() {
        console.log("connect. endpoint: "+this.endpointValue+" allTags: "+this.alltagsValue)
    }
    
    handleSelection(event) {
        const selectedValue = parseInt(event.target.value);
        this.doUpdate({'new_tags':[...this.tagsValue, selectedValue], 'add_tag': selectedValue});
    }
    
    removeTag(event) {
        let tagid = parseInt(event.params.tagid);
        this.doUpdate( { 'new_tags': this.tagsValue.filter(item => item !== tagid), 'remove_tag': tagid})
    }
    
    doUpdate(o)
    {
        Turbo.visit(this.endpointValue, {
            frame: this.element.closest("turbo-frame").id,
            action: 'replace',
            method: 'post',
            data: JSON.stringify(o)
        });
        
        //
        // Turbo.fetch(this.endpointValue, {
        //     method: "post",
        //     body: JSON.stringify(o),
        //     headers: {
        //         "Content-Type": "application/json",
        //         "Accept": "text/vnd.turbo-stream.html, text/html, application/xhtml+xml"
        //     }
        // }).then(response => {
        //     if (response.ok) {
        //         return response.text();
        //     }
        //     throw new Error('Network response was not ok.');
        // }).then(html => {
        //     Turbo.renderStreamMessage(html);
        // }).catch(error => {
        //     console.error('There was a problem with the Turbo request:', error);
        // });

    }
}