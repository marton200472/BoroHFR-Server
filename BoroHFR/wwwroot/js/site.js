var forms=$(".coolform")
$(forms).addClass("d-flex flex-column");
$("button[type=submit]",forms).addClass("mx-auto px-3 my-3");


$(forms).submit(function (x) {
    var a=$("button[type=submit]",x.currentTarget);
    $(a).replaceWith('<span class="spinner-grow spinner-grow-sm mx-auto my-3" role="status" aria-hidden="true"></span>');
    
});

async function postData(url = "", data = {}) {
    const response = await fetch(url, {
      method: "POST",
      mode: "same-origin", // no-cors, *cors, same-origin
      headers: {
        "Content-Type": "application/json",
        "Accept":"text/html"
      },
      body: JSON.stringify(data) // body data type must match "Content-Type" header
    });
    return response.text();
  }