//vueComponents.js

const ObjectTable = {
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
                    <object-table :record="value" :list="list"></object-table>
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
</div>
`
}

const SearchTable = {
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

export default { SearchTable, ObjectTable };