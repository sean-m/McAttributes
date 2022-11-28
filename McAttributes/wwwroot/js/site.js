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


class FilterBuilder {
    startMatch = []
    endMatch = []
    eqMatch = []
    containsMatch = []

    constructor(operator='or') {
        this.operator = operator;
    }

    addStartMatch(property) {
        if (property == null || property == undefined) {
            return this;
        }

        if (Array.isArray(property)) {
            this.startMatch = this.startMatch.concat(property)
        } else {
            this.startMatch.push(property)
        }
        return this;
    }

    addEqMatch(property) {
        if (property == null || property == undefined) {
            return this;
        }

        if (Array.isArray(property)) {
            this.eqMatch = this.eqMatch.concat(property)
        } else {
            this.eqMatch.push(property)
        }
        return this;
    }

    addEndMatch(property) {
        if (property == null || property == undefined) {
            return this;
        }

        if (Array.isArray(property)) {
            this.endMatch = this.endMatch.concat(property)
        } else {
            this.endMatch.push(property)
        }
        return this;
    }

    addContainsMatch(property) {
        if (property == null || property == undefined) {
            return this;
        }

        if (Array.isArray(property)) {
            this.containsMatch = this.containsMatch.concat(property)
        } else {
            this.containsMatch.push(property)
        }
        return this;
    }

    setOperator(operator) {
        this.operator = operator
        return this;
    }

    buildFilterString(searchTerm) {
        if (searchTerm === null || searchTerm === undefined || searchTerm.length === 0) {
            return null;
        }

        const predicates = []

        for (let t of this.startMatch) {
            predicates.push(`startswith(tolower(${t}), tolower('${searchTerm}'))`)
        }
        for (let t of this.endMatch) {
            predicates.push(`endswith(tolower(${t}), tolower('${searchTerm}'))`)
        }
        for (let t of this.eqMatch) {
            predicates.push(`tolower(${t}) eq tolower('${searchTerm}')`)
        }
        for (let t of this.containsMatch) {
            predicates.push(`contains(${t}, '${searchTerm}')`)
        }

        let filterPredicate = predicates.join(` ${this.operator} `)
        return `$filter=${filterPredicate}`
    }
}

class ObjectSearchContext {
    pageNumber= 0;
    pageSize= 10;
    paginate= true;
    results= [];
    resultCount= 0;
    searchTerm= "";
    searchError= null;
    apiPath = { odata: null, api: null };

    getQueryString(skip = 0) {
    }

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
    }

    executeSearch(skip = 0) {
        let queryString = this.getQueryString();
        if (skip) { queryString = this.getQueryString(skip); }
        console.log(`Query string: ${this.apiPath.odata}?${queryString}`);

        $.getJSON(`${this.apiPath.odata}?${queryString}`, null,
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
    }

    loadNextSet() {
        // Grab the current page value advanced by one, we'll only update
        // the pageNumber value if the lookup succeeded.
        var currentPage = this.pageNumber + 1;

        // We want to skip over the results we've already fetched.
        if (this.executeSearch(this.results.length)) {
            this.pageNumber = currentPage;
        }
    }

    clearResults() {
        this.results = [];
        this.resultCount = 0;
    }
}

class UserSearchContext extends ObjectSearchContext {
    constructor() {
        super()

        this.apiPath = {
            api: "/api/User",
            odata: "/odata/User"
        }
    }

    getQueryString(skip = 0) {
        let queryString = ''
        let startMatch = ['mail']
        let eqMatch = ['employeeId', 'preferredSurname', 'preferredGivenName']
        const filterBuilder = new FilterBuilder('or').addStartMatch(startMatch).addEqMatch(eqMatch);

        if (this.paginate) {
            queryString = `$count=true&$top=${this.pageSize}&$skip=${skip}`;
        }

        let filterString = filterBuilder.buildFilterString(this.searchTerm);
        if (filterString) {
            queryString = `${queryString}&${filterString}`
        }
        return queryString;
    }
}

class IssueSearchContext extends ObjectSearchContext {
    constructor() {
        super()

        this.apiPath = {
            api: "/api/IssueLogEntry",
            odata: "/odata/IssueLogEntry"
        }
    }

    getQueryString(skip = 0) {
        let queryString = ''
        let contains = ['attrName']
        let eqMatch = ['status']
        const filterBuilder = new FilterBuilder('or').addContainsMatch(contains).addEqMatch(eqMatch);

        if (this.paginate) {
            queryString = `$count=true&$top=${this.pageSize}&$skip=${skip}`;
        }

        let filterString = filterBuilder.buildFilterString(this.searchTerm);
        if (filterString) {
            queryString = `${queryString}&${filterString}`
        }
        return queryString;
    }
}

class StargateSearchContext extends ObjectSearchContext {
    constructor() {
        super()

        this.apiPath = {
            api: "/api/Stargate",
            odata: "/odata/Stargate"
        }
    }

    getQueryString(skip = 0) {
        console.log("Dialing gate...")
        return "$count=true"
    }
}

const { createApp } = Vue

const appDefinition = {
    data() {
        return {
            currentTab: 'userForm',
            currentUserSearch: new UserSearchContext(),
            currentIssueSearch: new IssueSearchContext(),
            stargateSearch: new StargateSearchContext(),
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
        // User search supporting methods
        searchForUsers() {
            this.currentUserSearch.clearResults();
            this.currentUserSearch.executeSearch();
        },
        loadMoreUserResults() {
            this.currentUserSearch.loadNextSet();
        },
        // Issue handling support methods
        searchForIssues() {
            this.currentIssueSearch.clearResults();
            this.currentIssueSearch.executeSearch();
        },
        searchStargate() {
            this.stargateSearch.clearResults();
            this.stargateSearch.executeSearch();
        },
        showIssuesForUser(term) {
            console.log("Issue lookup: " + term)
            this.currentIssueSearch.searchTerm = term;
            this.searchForIssues();
            this.currentTab = 'issueForm';
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