# SproutForms

SproutForms is an open source, free to use Forms package for Umbraco. It supports forms in code, forms in the backoffice, various fields and flows that can all be defined either in code or in the backoffice.

# Beta notice
This package is still in beta (and preview in NuGet). This is because I want the package out there to ensure people can start using it, but there are still some things not entirely polished. You can see it as a prototype or MVP which still needs a bit of work/love to get there. By bringing out this beta, you are also able to give feedback on the package and influence the way that the package will be developed.

Another thing is that I'll most likely also introduce a paid package alongside this one. The idea is that the free package provides small websites with an easy to use forms solution that doesn't cost any money. It is also a framework where developers can build/extend upon for their own clients. The paid package will mostly be focused on enterprise and will include the following things at first:
- More flow types focused on integrating with big services.

By letting you know now, I hope it won't feel as a rugpull later on.

# Installation

Installation is simple with just a few steps being required.

1) Download the Nuget package `Install-Package SproutForms.Umbraco`
2) Add the "SproutForms" section to the user/user groups that you want to have this functionality
3) Add the following scripts to your <head> tag:
```
<link rel="stylesheet" href="/forms/forms-layout.css" />
<link rel="stylesheet" href="/forms/forms-default-theme.css" />
<script src="/forms/forms.js"></script>
```
4) Add the following to your "_ViewImports.cshtml": `@addTagHelper *, SproutForms.Umbraco.Core`
5) You can now render your forms by using `<vc:render-form form-alias="testFileForm"></vc:render-form>` or `<vc:render-form form-id="[guid]"></vc:render-form>`

## Contents

SproutForms currently supports these field types:
- Textbox (small question)
- Textarea (larger question)
- Email
- Date
- File
- Select (dropdown)
- Radio buttons
- Hidden

And supports the following flows:
- Send email

Data is automatically stored in the database and can be viewed as such in the backoffice. You also have the option to either show a message or redirect the user to a different page.
