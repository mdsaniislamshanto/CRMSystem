// Lead Status Pie Chart

const leadStatusCanva = document.getElementById("leadStatusChart");

if (leadStatusCanva) {

    new Chart(leadStatusCanva, {

        type: "pie",

        data: {

            labels: [

                "New",
                "Assigned",
                "Accepted",
                "In Progress",
                "Completed",
                "Rejected"

            ],

            datasets: [{

                data: [

                    leadStatusData.newLeads,

                    leadStatusData.assignedLeads,

                    leadStatusData.acceptedLeads,

                    leadStatusData.inProgressLeads,

                    leadStatusData.completedLeads,

                    leadStatusData.rejectedLeads

                ],

                backgroundColor: [

                    "#6B7280",
                    "#F59E0B",
                    "#2563EB",
                    "#0EA5E9",
                    "#16A34A",
                    "#DC2626"

                ],

                borderColor: "#FFFFFF",

                borderWidth: 2

            }]

        },

        options: {
            responsive: true,
            maintainAspectRatio: false,

            plugins: {
                legend: {
                    position: "bottom",
                    labels: {
                        usePointStyle: true,
                        pointStyle: "circle",
                        padding: 15
                    }
                }
            }
        }
    });

}