# Pages

A form can be split into pages. forms.js shows one page at a time with Previous and Next buttons, and a progress list of the page titles. Next checks the current page's fields before it moves on, first in the browser and then on the server (`POST /api/forms/{id}/pages/{index}/validate`), so rules only the server checks, such as the email format, show on the page they belong to. Uploads are only checked when the form is submitted. The form is still submitted once, from the last page, and an error on an earlier page takes the visitor back to it. Without JavaScript every page shows, one after the other.

Headless front-ends get the same behaviour from the form engine, with `POST entries/{id}/pages/{index}/validate` on the headless API; see [Install headless](../getting-started/headless/README.md).

## In the backoffice

The Build tab shows the form's pages above the canvas. Click a page to edit its fields and, in the side panel, its title, button labels and when it's shown. Drop a field on a page to move it there, or drag a page to reorder. The submit button's text and whether the progress steps show are on the Settings tab.

A form is checked when it's saved, and when a code-first form is registered: every field is placed on exactly one page, only a form's single page may be empty, and a condition only uses fields from earlier pages (or, for a field, its own page).

## In code

Add each page with `Page`. A page without a title shows as "Step n" in the progress list:

```csharp
new FormBuilder("order", "Order")
    .Page("About you", page => page
        .NextLabel("Continue")
        .Row(row => row.Col(12, col => col.Text("name", "Name").Required().Done())))
    .Page("Your order", page => page
        .PreviousLabel("Back")
        .Row(row => row.Col(12, col => col.Textarea("message", "Message").Done())))
    .SubmitLabel("Send order")
    .ShowProgress(false)            // leave out the progress list
    .Build();
```

A form built with only `Row` has a single page, and renders without page navigation. Rows can't follow a `Page`: once a form has pages, add rows to a page.

## Pages that depend on answers

A page can depend on earlier answers with `VisibleWhen`. When its conditions don't hold, the page is skipped and left out of the progress list, and its fields aren't validated, in the browser or on the server:

```csharp
.Page("Delivery address", page => page
    .VisibleWhen(c => c.Field("delivery", ConditionComparison.Equals, "home"))
    .Row(row => row.Col(12, col => col.Text("address", "Address").Required().Done())))
```

A page condition can also use a [variable](calculations.md), as long as the variable only depends on earlier pages. See [Field rules](field-rules.md#conditions) for what a condition can compare.

## Page change events

Every page change raises a `sproutforms:pagechange` event on the form, with the new and previous page index in `detail`:

```js
document.addEventListener("sproutforms:pagechange", e => window.scrollTo({ top: e.target.offsetTop }));
```

`@sproutforms/vue` emits `pagechange` on `<SproutForm>` instead, see [Vue](../getting-started/headless/vue.md#rendering-a-form).

## Styling

The pages and progress list are styled by `forms-layout.css` and `forms-default-theme.css`. The attributes forms.js sets on them, such as `data-sf-skipped` and `aria-current="step"`, are listed in [Styling](../styling.md#state-attributes).
