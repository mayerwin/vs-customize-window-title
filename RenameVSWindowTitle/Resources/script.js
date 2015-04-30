//http://stackoverflow.com/questions/200162/allow-only-copy-paste-context-menu-in-system-windows-forms-webbrowser-control/200194#200194
//<div id='ContextMenu' style='display: none; z-index: 1000; padding: 1px 10px; background-color: white; border: 2px solid lightgreen; position: absolute;'><a href='#' id='CopyBtn' style='display: block; color: black; text-decoration: none;'>Copy</a></div>

function selectText(obj) { // adapted from Denis Sadowski (via StackOverflow.com)
    var range;
    if (document.selection) {
        range = document.body.createTextRange();
        range.moveToElementText(obj);
        range.select();
    }
    else if (window.getSelection) {
        range = document.createRange();
        range.selectNode(obj);
        window.getSelection().addRange(range);
    }
    copy = document.selection.createRange();
    copy.execCommand("Copy");
    return false;
}