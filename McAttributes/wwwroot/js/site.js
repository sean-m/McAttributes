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
                resultCount: 0,
                searchTerm: "",
                getQueryString(skip = 0) {
                    let filterString = `$count=true&$filter=startswith(mail,'${this.searchTerm}') or startswith(employeeId, '${this.searchTerm}') or startswith(preferredSurname,'${this.searchTerm}') or startswith(preferredGivenName,'${this.searchTerm}')`;
                    if (this.paginate) {
                     return `$top=${this.pageSize}&$skip=${skip}&${filterString}`;
                    }
                    return filterString;
                },
                updateResultSet(json) {
                    this.resultCount = json['@odata.count'];
                    var list = json.value;
                    if (Array.isArray(list)) {
                        this.results = this.results.concat(list)
                        return true;
                    } else {
                        this.results.push(list)
                        return true;
                    }
                    return false;
                },
                searchForUsers(skip=0) {
                    let queryString = this.getQueryString();
                    if (skip) { queryString = this.getQueryString(skip); }
                    console.log(queryString);
                    $.getJSON(`${uriUser.odata}?${queryString}`,
                        json => {
                            this.updateResultSet(json);
                        }
                    );
                },
                loadNextSet() {
                    // Grab the current page value advanced by one, we'll only update
                    // the pageNumber value if the lookup succeeded.
                    var currentPage = this.pageNumber + 1;

                    // We want to skip over the results we've already fetched.
                    if (this.searchForUsers(this.results.length)) {
                        this.pageNumber = currentPage;
                    }
                },
                clearResults() {
                    this.results = [];
                    this.resultCount = 0;
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
            },
            searchButton: {
                class: "button"
            }
        }
    },
    methods: {
        clearResults() {
            this.currentUserSearch.clearResults();
        },
        searchForUsers() {
            this.clearResults();
            this.currentUserSearch.searchForUsers();
        },
        loadMoreUserResults() {
            this.currentUserSearch.loadNextSet();
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