// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.


const { createApp } = Vue

class ResultPager {

}

const uriUser = {
    api: "/api/User",
    odata: "/odata/User"
}

createApp({
    data() {
        return {
            searchTerm: "",
            currentUserSearch: {
                page: 0,
                results: [],
                getQueryString() {
                    return "$top=10&$skip=" + (this.page * 10).toString();
                }
            },
            includeResolved: false,
            issues: {
                count() { return this.issueList.count },
                issueList: [],
            },
            currentIssue: {
                edit: false,
                model: {}
            }
        }
    },
    methods: {
        searchForUsers() {
            this.currentUserSearch.page++

            $.getJSON(`${uriUser.odata}?${this.currentUserSearch.getQueryString()}`,
                json => {
                    if (Array.isArray(json.value)) {
                        this.currentUserSearch.results = this.currentUserSearch.results.concat(json.value)
                    } else {
                        this.currentUserSearch.results.push(json.value)
                    }
                }
            );
        },
        searchForIssues() {

        },
        newIssueEntry() {
            this.currentIssue = {
                edit: false,
                model: {
                    attrName:'',
                    created:'',
                    alertHash:'',
                    status:'',
                    description:'',
                    notes:''
                }
            }
        }
    }
}).mount('#app')