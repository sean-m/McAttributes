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
            currentUserSearch: {
                pageNumber: 0,
                pageSize: 10,
                paginate: true,
                results: [],
                searchTerm: "",
                getQueryString() {
                    if (this.paginate) {
                     return `$count=true&$top=${this.pageSize}&$skip=${this.pageNumber * this.pageSize}&$filter=startswith(mail,'${this.searchTerm}')`;
                    }
                    return `$filter=startswith(mail,'${this.searchTerm}')`;
                },
                updateResultSet(json) {

                    var list = json.value;
                    if (Array.isArray(list)) {
                        this.results = this.results.concat(list)
                    } else {
                        this.results.push(list)
                    }
                },
                clearResults() {
                    this.results = [];
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
        clearResults() {
            this.currentUserSearch.clearResults();
        },
        searchForUsers() {
            this.currentUserSearch.page++

            $.getJSON(`${uriUser.odata}?${this.currentUserSearch.getQueryString()}`,
                json => {
                    this.currentUserSearch.updateResultSet(json);
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