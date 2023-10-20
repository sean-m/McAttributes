class CohortDescription {
    constructor() {
        this.id = null;
        this.status = 'pending';
        this.associateCohorts = false;
        this.cohortMembers = [];
        this.description = null;
    }
}

class CohortMember {
    constructor() {
        this.memberId = '';
        this.cohortBucket = -1;
    }
}

class AccountBucket {
    constructor(accounts) {
        this.accounts = accounts
        this.accounts.forEach(a => { a['bucket'] = 0; a['approvalStatus'] = ''; })
        this.associateCohorts = false;
        this.approvals = null;
    }

    asCohort() {
        var cohort = new CohortDescription();
        cohort.associateCohorts = this.associateCohorts;

        this.accounts.forEach(m => {
            var member = new CohortMember();
            member.id = m.aadId;
            member.cohortBucket = m.bucket;
            cohort.cohortMembers.push(member);
        });

        return cohort;
    }

    asApprovalSet(issueId) {
        var result = [];

        for (a of this.accounts) {
            var approval = new AlertApproval();
            approval.userId = a.id;
            approval.alertId = issueId;
            approval.status = a.approvalStatus;
            result.push(approval);
        }

        return result;
    }

    setApprovals(approvals) {
        if (approvals != null && approvals != undefined) {
            if (Array.isArray(approvals)) {
                for (var ap of approvals) {
                    for (var act of this.accounts) {
                        if (act.id == ap.userId) {
                            act.approvalStatus = ap.status;
                        }
                    }
                }
            }
            this.approvals = approvals;
        }
    }
}

class AlertApproval {
    constructor(userId, alertId, status) {
        this.userId = userId;
        this.alertId = alertId;
        this.status = status;
        this.id = 0;
    }
}

const apiCohort = '/api/Cohort';
const apiApproval = '/api/AlertApproval';

const bucketAppDefinition = {
    data() {
        return {
            buckets: [],
            bucketIndex: 0,
            columns: ['displayName', 'tenant', 'enabled', 'deleted', 'preferredGivenName', 'preferredSurname', 'mail', 'creationType', 'created'],
            bucket: new AccountBucket(window.accountsJson),
            cohorts: [],
            issueAlert: window.issueAlert,
            errorLog: [],
            saveButtonText: "Save Bucket",
        }
    },
    methods: {
        autoBucket() {
            this.getBucketsFromCohort();

            let nativeAccounts = this.bucket.accounts.filter(a =>
                !(a.creationType != null && a.creationType != undefined)
                && !(a.upn.startsWith("pa_")) && !(a.upn.startsWith("ta_")) && !(a.upn.startsWith("ra_")))

            if (nativeAccounts) {
                this.bucketIndex = nativeAccounts.length + 1
            }

            var boffset = 1;
            for (a of nativeAccounts) {
                for (b of this.bucket.accounts) {
                    if (b.id === a.id) {
                        b.bucket = boffset
                        continue
                    }

                    if (b.mail == a.mail) {
                        b.bucket = boffset
                    }
                }
                boffset++;
            }
            this.colorizeTableCells();
        },
        saveBucketApprovals() {

            let approvals = this.bucket.asApprovalSet(this.issueAlert.id);
            if (!approvals) {
                console.log("asApprovalSet function does't return a value. Can't post this to the server.");
                return;
            } else {
                console.log(approvals);
                // API expects the body fo the request to be a collection,
                // approvals needs to be stringified here or jquery will helpfully
                // dispatch a request per element.
                var approvalsListString = JSON.stringify(approvals);
                $.ajax({
                    url: apiApproval,
                    type: "PATCH",
                    data: approvalsListString,
                    contentType: "application/json",
                    accepts: "application/json",
                    dataType: "json",
                    success: r => {
                        console.log(r)
                        this.saveButtonText = "Save Bucket Complete!"
                        window.setTimeout(() => {
                            this.saveButtonText = "Save Bucket";
                        }, 5000);
                    },
                    error: e => {
                        console.log(`error: ${e}`);
                        this.errorLog.push(e);
                    }
                });
            }
        },
        getBucketsFromCohort() {
            let apiUrl = `${apiApproval}?alertId=${this.issueAlert.id}`;
                
            $.ajax({
                url: apiUrl,
                type: "GET",
                contentType: "application/json",
                accepts: "application/json",
                dataType: "json",
                success: r => {
                    this.bucket.setApprovals(r);
                },
                error: e => {
                    console.log(`error: ${e}`);
                    this.errorLog.push(e);
                }
            });
        },
        setBucketGroup(bucket, value) {
            console.log(`Approve bucket ${bucket}`)
            for (a of this.bucket.accounts) {
                if (a.bucket == bucket) {
                    a.approvalStatus = value;
                }
            }
        },
        startDrag(evt, item) {
            console.log("Dragging " + item.DisplayName);
            evt.dataTransfer.dropEffect = 'move';
            evt.dataTransfer.effectAllowed = 'move';
            evt.dataTransfer.setData('itemID', item.Id);
        },
        onDrop(evt, list) {
            const itemID = evt.dataTransfer.getData('itemID');
            if (list.name && list.name.startsWith("Bucket")) {
                console.log(list);
            }
        },
    },
    watch: {
        'cohorts'(added) {
            console.log(add);
        }
    },
};

const bucketApp = createApp(bucketAppDefinition);
bucketApp.config.compilerOptions.isCustomElement = (tag) => tag.includes('-');
bucketApp.mount('#associatedPartial');