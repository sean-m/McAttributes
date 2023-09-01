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
    select = []

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

    addPropertySelection(property) {
        if (property == null || property == undefined) {
            return this;
        }

        if (Array.isArray(property)) {
            this.select = this.select.concat(property)
        } else {
            this.select.push(property)
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

        let filterPredicate = `$filter=${predicates.join(` ${this.operator} `)}`

        let filterString = filterPredicate

        if (this.select && this.select.length > 0) {
            filterString = `${filterString}&$select=${this.select.join(',')}`
        }

        return filterString
    }

    buildFilterStringWithList(list) {
        // Warning! This gets out of hand really quickly and can build a gnarly query string
        // that straig up wont work. Be sure you're using it correctly.
        if (list === null || list === undefined) {
            return null;
        }

        if (!Array.isArray(list)) {
            return this.buildFilterString(list);
        }

        const predicates = []

        for (let term of list) {
            for (let t of this.startMatch) {
                predicates.push(`startswith(tolower(${t}), tolower('${term}'))`)
            }
            for (let t of this.endMatch) {
                predicates.push(`endswith(tolower(${t}), tolower('${term}'))`)
            }
            for (let t of this.eqMatch) {
                predicates.push(`tolower(${t}) eq tolower('${term}')`)
            }
            for (let t of this.containsMatch) {
                predicates.push(`contains(${t}, '${term}')`)
            }
        }
        
        let filterPredicate = `$filter=${predicates.join(` ${this.operator} `)}`

        let filterString = filterPredicate

        if (this.select && this.select.length > 0) {
            filterString = `${filterString}&$select=${this.select.join(',')}`
        }

        return filterString
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

    async executeSearch(skip = 0) {
        let queryString = this.getQueryString();
        if (skip) { queryString = this.getQueryString(skip); }
        console.log(`Query string: ${this.apiPath.odata}?${queryString}`);

        $.getJSON(`${this.apiPath.odata}?${queryString}`, null,
            json => {
                this.updateResultSet(json);
            }
        ).fail(e => {
            var errorMessageObj = null;
            if (e.responseText && (errorMessageObj = JSON.parse(e.responseText))) {
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
    constructor(global) {
        super()

        this.apiPath = {
            api: "/api/User",
            odata: "/odata/User"
        }

        this.parent = global;
        this.sgApiPath = new StargateSearchContext().apiPath;
    }

    getQueryString(skip = 0) {
        let queryString = '$count=true'
        let startMatch = ['mail']
        let eqMatch = ['employeeId', 'preferredSurname', 'preferredGivenName']
        const filterBuilder = new FilterBuilder('or').addStartMatch(startMatch).addEqMatch(eqMatch);

        if (this.paginate) {
            queryString += `&$top=${this.pageSize}&$skip=${skip}`;
        } else {
            queryString += `&$top=100`
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
        
        let queryString = '$count=true'
        let eqMatch = ['localId', 'globalId']
        const filterBuilder = new FilterBuilder('or').addEqMatch(eqMatch);

        if (this.paginate) {
            queryString += `&$top=${this.pageSize}&$skip=${skip}`;
        }

        let filterString = filterBuilder.buildFilterString(this.searchTerm);
        if (filterString) {
            queryString = `${queryString}&${filterString}`
        }
        return queryString;
    }

    
    // async executeSearch(skip = 0) {
    //     let queryString = this.getQueryString();
    //     if (skip) { queryString = this.getQueryString(skip); }
    //     console.log(`Query string: ${this.apiPath.odata}?${queryString}`);

    //     /*
    //         The Stargate table is just a bridge table across many accounts. It's the 'identity' record
    //         to the account's 'persona' records. In short, they're worthless to look at so should start
    //         with a user account search then cross-walking through the Stargate records to resolve
    //         associated accounts.
    //     */

    //     let userSearch = new UserSearchContext();
    //     userSearch.searchTerm = this.searchTerm;
    //     userSearch.pageSize = 100;
    //     userSearch.paginate = false;
    //     let userResults = await axios.get(`${userSearch.apiPath.odata}?${userSearch.getQueryString()}&$select=AadId,Upn`);

    //     let userIds = userResults.data.value.map(x => { return x.AadId; });
    //     let crosswalk = this.localToGlobalMap;
    //     let globalIds = userIds.map((i) => { 
    //         return crosswalk[i]; 
    //     });

    //     console.table(globalIds);

    //     $.getJSON(`${this.apiPath.odata}?${queryString}`, null,
    //         json => {
    //             this.updateResultSet(json);
    //         }
    //     ).fail(e => {
    //         var errorMessageObj = null;
    //         if (e.responseText && (errorMessageObj = JSON.parse(e.responseText))) {
    //             this.searchError = errorMessageObj;
    //         }
    //         else { this.searchError = e; }
    //     });
    // } 
    
}

const { createApp } = Vue

const appDefinition = {
    data() {
        return {
            currentTab: 'userForm',
            currentTabStyleClass: 'tab-content flex-column',
            currentTabExpand: false,
            currentUserSearch: new UserSearchContext(),
            currentIssueSearch: new IssueSearchContext(),
            stargateSearch: new StargateSearchContext(),
            stargatePath: {
                endpoints: [],
                addEndpoint(endpoint) {
                    this.endpoints.push(endpoint);
                },
                removeEndpoint(endpoint) {
                    this.endpoints = this.endpoints.filter(function (value, index, arr) {
                        return value.Id != endpoint.Id;
                    });
                },
                clear() {
                    this.endpoints = []
                },
            },
            localToGlobalMap: {},
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
            },
            selectedUser: null
        }
    },
    methods: {
        clearResults() {
            switch (this.currentTab) {
                case 'userForm':
                    if (this.selectedUser) { this.selectedUser = null; }
                    if (this.currentUserSearch) { this.currentUserSearch.clearResults(); }
                    this.resolveCurrentTabClass();
                    break;
                case 'issueForm':
                    if (this.currentIssueSearch && this.currentTab == 'issueForm') { this.currentIssueSearch.clearResults(); }
                    break
            }
        },
        resolveCurrentTabClass() {
            this.currentTabStyleClass = this.selectedUser === null && this.currentUserSearch.resultsCount != 0
                ? 'tab-content flex-column'
                : 'tab-content flex-column overflow-scroll w-25';
        },

        // User search supporting methods
        searchForUsers() {
            this.currentUserSearch.clearResults();
            this.currentUserSearch.executeSearch();
        },
        selectUserRow(user) {
            if (this.selectedUser == user) { this.selectedUser = null; }
            else { this.selectedUser = user; }
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
        },
        async crosswalkGlobalIds (localIds) {
            console.log("Run global crosswalk")
    
            let uri = "/odata/Stargate";
    
            let queryBuilder = new FilterBuilder('or').addEqMatch('localId');
            
            const chunkSize = 10;

            for (let i = 0; i < localIds.length; i += chunkSize) {
                const chunk = localIds.slice(i, i + chunkSize);
                let filter = queryBuilder.buildFilterStringWithList(chunk);
                filter = filter + "&$select=globalId,localId"
                
                let crosswalk = await axios.get(`${uri}?${filter}`);
                if (crosswalk.status == 200) {
                    for (let record of crosswalk.data.value) {
                        this.localToGlobalMap[record.LocalId] = record.GlobalId;
                    }
                }
            }
        },
    },
    watch: {
        'currentUserSearch.results'(latestResults, previousResults) {
            let ids = []
            ids = ids.concat(latestResults.map(x => { return x.AadId; }));
            this.crosswalkGlobalIds(ids);
        },
        'selectedUser'() {
            this.resolveCurrentTabClass();
        }
    },
};

const app = createApp(appDefinition);
app.config.compilerOptions.isCustomElement = (tag) => tag.includes('-')
app.component('SearchTable', SearchTable);
app.mount('#app');