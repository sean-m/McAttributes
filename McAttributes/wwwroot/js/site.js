// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

let ObjectTable = {
    props: ['record', 'title', 'expand', 'list'],
    data() {
        return {};
    },
    methods: {
        expandOnCase(input) {
            return input.replace(/([a-z])([A-Z])/g, '$1 $2')
        }
    },
    template: `<div>
    <table class="table table-responsive-sm">
        <thead v-if="title">
            <tr><td><strong>{{ title }}</strong></td><td></td></tr>
        </thead>
        <tbody>
            <tr v-for="(value, key, i) in record">
                <td>{{ expandOnCase(key) }}</td>
                <td v-if="expand && expand.includes(key)">
                    <ObjectTable :record="value" :list="list"></ObjectTable>
                </td>
                <td v-else-if="list && list.includes(key)">
                    <ul class="no-bullets">
                        <li v-for="(entry) in value.sort()">{{ entry }}</li>
                    </ul>
                </td>
                <td v-else>{{ value }}</td>
            </tr>
        </tbody>
    </table>
</div>`
}

let SearchTable = {
    props: ['records', 'columns'],
    data() {
        return {};
    },
    methods : {        
        expandOnCase(input) {
            return input.replace(/([a-z])([A-Z])/g, '$1 $2')
        }
    },
    template: `
    <table class="table">
    <tr>
        <th v-for="(value, key, i) in records[0]">{{ expandOnCase(key) }}</td>
    </tr>
    <tr v-for="value of records">
        <td v-for="(val, key, i) in value">{{ val }}</td>
    </tr>
</table>
    `
}

const { createApp } = Vue

const uriUser = {
    api: "/api/User",
    odata: "/odata/User"
}

class FilterBuilder {
    constructor(startMatch, eqMatch, operator='or') {
        this.startMatch = Array.isArray(startMatch) ? startMatch : [startMatch];
        this.eqMatch = Array.isArray(eqMatch) ? eqMatch : [eqMatch];
        this.operator = operator;
    }

    addStartMatch(property) {
        this.init();
        this.startMatch.push(property)
    }

    addEqMatch(property) {
        this.init();
        this.eqMatch.push(property)
    }

    buildFilterString(searchTerm) {
        const predicates = []
        for (let t of this.startMatch) {
            predicates.push(`startswith(tolower(${t}), tolower('${searchTerm}'))`)
        }
        for (let t of this.eqMatch) {
            predicates.push(`tolower(${t}) eq tolower('${searchTerm}')`)
        }

        let filterPredicate = predicates.join(` ${this.operator} `)
        return filterPredicate
    }
}

const appDefinition = {
    data() {
        return {
            currentUserSearch: {
                pageNumber: 0,
                pageSize: 10,
                paginate: true,
                results: [],
                resultCount: 0,
                searchTerm: "",
                searchError: null,
                getQueryString(skip = 0) {
                    let startMatch = ['mail']
                    let eqMatch = ['employeeId', 'preferredSurname', 'preferredGivenName']
                    const opOr = 'or'

                    const filterBuilder = new FilterBuilder(startMatch, eqMatch, opOr);

                    
                    let filterString = `$filter=${filterBuilder.buildFilterString(this.searchTerm)}`;
                    if (this.paginate) {
                        return `$count=true&$top=${this.pageSize}&$skip=${skip}&${filterString}`;
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
                        if (this.results.push(list)) {
                            return true;
                        }
                    }
                    return false;
                },
                searchForUsers(skip = 0) {
                    let queryString = this.getQueryString();
                    if (skip) { queryString = this.getQueryString(skip); }
                    console.log(queryString);

                    $.getJSON(`${uriUser.odata}?${queryString}`, null,
                        json => {
                            this.updateResultSet(json);
                        }
                    ).fail(e => {
                        var errorMessageObj = null;
                        if (errorMessageObj = JSON.parse(e.responseText)) {
                            this.searchError = errorMessageObj;
                        }
                        else { this.searchError = e; }
                    });
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
        },
        expandOnCase(input) {
            return input.replace(/([a-z])([A-Z])/g, '$1 $2')
        }
    }
};

const app = createApp(appDefinition);
app.config.compilerOptions.isCustomElement = (tag) => tag.includes('-')
app.component('SearchTable', SearchTable);
app.mount('#app');