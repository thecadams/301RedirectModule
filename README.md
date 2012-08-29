301RedirectModule
=================

An improved version of the Sitecore 301 redirect module. Original version was created by Chris Castle, available at [http://trac.sitecore.net/301RedirectModule](http://trac.sitecore.net/301RedirectModule)

Improvements by Chris Adams, chris@cadams.com.au

## Changelog ##

**Version 1.1.1:**

* Support for `encodeNames="true"` (used for Sitecore friendly URLs; most notably, this will make redirects work when dashes in URLs correspond to spaces in item names)

**Version 1.1:**

* Redirects for items which already exist
* Case-insensitive URL matching
* Path and query matching

## New Features ##

* Works with Sitecore friendly URLs!
* Ability to process redirects for specific items which already exist. Useful if you have an existing item, but need to change part of a query string. Beware of the performance implications as the list of items is examined against every request.
* **Case-insensitive matching** of URLs
* Match **the path and query** of a request, ignoring the hostname portion of the user's request. Useful if you need your match to work in multiple environments but don't want to list them in the redirect item.

## Examples ##
##### Match only the path, dropping the query string #####
- Requested Expression: `^/MovedItem/?`
- Source Item: `/sitecore/content/MySiteRoot/Destination`

##### Replace some of the query string but don't redirect away #####
Under the Content tab of `/sitecore/System/Modules/Redirect Module`, Edit the field **Items Which Always Process Redirects** and add an item, eg. `/sitecore/content/MySiteRoot/SomeItem/Path`

- Requested Expression: `^/SomeItem/Path/?\?(.*)name=OldValue(.*)`
- Source Item: `/sitecore/content/MySiteRoot/SomeItem/Path?$1name=NewValue$2`

##### Improving SEO: Remove .aspx extension #####
- Requested Expression: `^/MovedItem(.aspx)?$`
- Source Item: `/sitecore/content/MySiteRoot/Destination`

##### Preserving the query string #####
- Requested Expression: `^/MovedItem(?<OptionalQueryString>\?.*)?$`
- Source Item: `/sitecore/Content/MySiteRoot/Destination${OptionalQueryString}`

##### Move an area of your site #####
- Requested Expression: `^/MovedSiteArea(?<Path>/.*?)?(.aspx)?(?<OptionalQueryString>\?.*)?$`
- Source Item: `/sitecore/content/MySiteRoot/NewLocationOfSiteArea${Path}${OptionalQueryString}`