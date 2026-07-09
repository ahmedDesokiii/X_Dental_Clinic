$(document).ready(function () {
    $('#tableUser tfoot th').each(function () {
        var title = $(this).text();
        $(this).html('<input type="text" placeholder="بحث بـ ' + title + '" class="form-control form-control-sm" />');
    });

    var table = $('#tableUser').DataTable({
        autoWidth: true,
        responsive: true,
        pageLength: 10,
        lengthMenu: [10, 25, 50, 100, 200, 400, 1000],
        
        ordering: false,
        
        layout: {
            topStart: {
                search: {
                    placeholder: 'ابحث هنا... !'
                }
            },
            topEnd: {
                pageLength: {
                    menu: [10, 25, 50, 100, 200, 400, 1000]
                }
            },
            bottomEnd: {
                paging: {
                    buttons: 6
                }
            }
        },
        language: {
            search: '<i class="bi bi-search"></i>',
            searchPlaceholder: 'ابحث هنا... !',
            lengthMenu: '_MENU_',
            info: 'إظهار _START_ إلى _END_ من أصل _TOTAL_ سجل',
            zeroRecords: 'لم يعثر على أي سجل',
            paginate: {
                first: 'الأول',
                previous: 'السابق',
                next: 'التالي',
                last: 'الأخير'
            }
        }
    });

    // 
    table.columns().every(function () {
        var that = this;

        $('input', this.footer()).on('keyup change clear', function () {
            if (that.search() !== this.value) {
                that.search(this.value).draw();
            }
        });
    });
});

// Apply the search
table.columns().every(function () {
    var that = this;
    $('input', this.footer()).on('keyup change', function () {
        if (that.search() !== this.value) {
            that.search(this.value).draw();
        }
    });
});

$('#tablePermissions').dataTable({

    columnControl: {
        target: 1,
        content: ['search']
    },
    "autoWidth": true,
    "responsive": true,
    "pageLength": 200,
    layout: {
        topStart: {
            search: {
                placeholder: 'ابحث هنا... !'
            }
        },
        topEnd: {
            pageLength: {
                menu: [10, 25, 50, 100, 200, 400, 1000]
            }
        },
        bottomEnd: {
            paging: {
                buttons: 6
            }
        }
    },
    language: {
        "autoWidth": true,
        "responsive": true,
        search: ' <i class=\"bi bi-search\"></i>', // Customize the search input label
        lengthMenu: ' _MENU_ ', // Customize the "Show X entries" label
        lengthMenu: ' _MENU_ ', // Customize the "Show X entries" label
        info: 'إظهار _START_ إلى _END_ من أصل _TOTAL_ سجل', // Customize the info text
        zeroRecords: 'لم يعثر على أي سجل', // No records message
        paginate: {
            first: 'الأول',
            previous: 'السابق',
            next: 'التالي',
            last: 'الأخير'
        }
    }
});

function Delete(id) {
    Swal.fire({
        title: lbTitleMsgDelete,
        text: lbTextMsgDelete,
        icon: 'error',
        showCancelButton: true,
        confirmButtonColor: '#d33',
        cancelButtonColor: 'secondary',
        confirmButtonText: lbconfirmButtonText,
        cancelButtonText: lbcancelButtonText,

    }).then((result) => {
        if (result.isConfirmed) {
            //window.location.href = `/Users/DeleteUser?Id=${id}`;
            window.location.href = `@Url.Action("DeleteUser","Users")?Id=${id}`;
        }
    })
}

Edit = (id, name,passUser,passSign) => {
    document.getElementById("title").innerHTML = lbTitleEdit;
    document.getElementById("btnSave").value = lbEdit;
    document.getElementById("UserId").value = id;
    document.getElementById("UserName").value = name;
    document.getElementById("passUser").value = passUser;
    document.getElementById("passSign").value = passSign;

}

Rest = () => {
    document.getElementById("title").innerHTML = lbAddNewUser;
    document.getElementById("btnSave").value = lbbtnSave;
    document.getElementById("UserId").value = "";
    document.getElementById("UserName").value = "";

}

ManageRoles = (id) => {
    document.getElementById("usrId").value = id;
}

