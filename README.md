301RedirectModule
=================

An improved version of the Sitecore 301 redirect module. Original version was created by Chris Castle, available at [http://trac.sitecore.net/301RedirectModule](http://trac.sitecore.net/301RedirectModule)

Improvements by [Chris Adams](http://www.cadams.com.au/), [Max Slabyak](https://github.com/maxslabyak) and [Mark Wiseman](https://github.com/mawiseman)

## Changelog ##

**Version 1.5:**

* Added missing English language versions on template fields.
* Complied against 8.2 dlls
* Changed Target Framework Version to 4.6.1
* Added Notifications to Content Editor
  - If an item is the target of redirects, you can see, manage, and delete them.
  - If the item is a redirect Url item, you can quickly navigate to the target.
* Added a handler to automatically generate redirects as pages are moved. This is controlled by a setting in the config file.

**Version 1.4:**

* Added the ability to select HTTP status code (301, 302)

**Version 1.3:**

* Added support for redirects to a raw Url - contributed by [Mark Wiseman](https://github.com/mawiseman)

**Version 1.2:**

* Added support for i18n and versioned redirects ([https://github.com/thecadams/301RedirectModule/pull/5/commits](https://github.com/thecadams/301RedirectModule/pull/5/commits)) - contributed by [Max Slabyak](https://github.com/maxslabyak)

**Version 1.1.2:**

* Fixed an exception when using exact URL matching on pattern items.

**Version 1.1.1:**

* Support for `encodeNames="true"` (used for Sitecore friendly URLs; most notably, this will make redirects work when dashes in URLs correspond to spaces in item names)

**Version 1.1:**

* Redirects for items which already exist
* Case-insensitive URL matching
* Path and query matching

## New Features ##

* Works with Sitecore friendly URLs!
* **Case-insensitive matching** of URLs
* Ability to process redirects for specific items which already exist. Useful if you have an existing item, but need to change part of a query string. Beware of the performance implications as the list of items is examined against every request.

![Redirecting when items already exist](http://blog.igloo.com.au/wp-content/uploads/2012/08/AlwaysRedirect.png)

* Match **the path and query** of a request, ignoring the hostname portion of the user's request. Useful if you need your match to work in multiple environments but don't want to list them in the redirect item.

![Redirecting and modifying the query string](http://blog.igloo.com.au/wp-content/uploads/2012/08/SiteAreaRedirect.png)

The regular expression above in the "Requested Expression" field will match any request where the path portion of the URL begins with /old-site-area. For instance, a request to http://mysite.com/old-site-area/homepage will be processed by this redirect. As a side-effect, the path portion under /old-site-area ("/homepage") will be saved for later in a capture group called Path. This is the "^/old-site-area(?<Path>/.*?)?" portion of the regular expression.

Because Sitecore accepts URLs ending with .aspx, this regular expression will also process a request to http://mysite.com/old-site-area/homepage.aspx. This is not compulsory but recommended, otherwise you have 2 URLs pointing to one page. This can cause search engines to penalise your content for being duplicated twice on your site. Removing the .aspx extension will help make your content look like it only lives in one place. This is the "(.aspx)?" portion of the regular expression.

Lastly this regular expression accepts (but does not require) a query string, so this regular expression will also match a request to http://mysite.com/old-site-area/homepage.aspx?thesky=blue. This is the "(?<OptionalQueryString>\?.*)?$" portion of the regular expression.

This looks complicated but it's not magic; it's just regular expressions. This is using .NET framework regular expression syntax. You can learn about regular expressions on Wikipedia and test a regular expression with Derek Slager's .NET Regular Expression tester.

OK, example time. A user just sent a request to http://mysite.com/old-site-area/homepage.aspx?thesky=blue. What happens?

1. The "Requested Expression" is tested and found to be a match, as per above.
2. The value of the "Source Item" field is substituted as needed. In this case, ${Path} will become "/homepage" and ${OptionalQueryString} will become "?thesky=blue", so the final value of "Source Item" for this request becomes "/sitecore/content/Site/New Site Area/homepage?thesky=blue". ${Path} and ${OptionalQueryString} are names you can see in the "Requested Expression" field.
4. Everything before the question mark is used to look up the path of an item in the Sitecore tree.
5. If an item is found, its friendly URL is retrieved. In this case, the URL will be http://mysite.com/new-site-area/homepage
6. The query string is put on the end. In this case, the URL will become http://mysite.com/new-site-area/homepage?thesky=blue
7. A 301 redirect is issued, which tells browsers and search engines that the URL the user requested (http://mysite.com/old-site-area/homepage.aspx?thesky=blue) can now be found at http://mysite.com/new-site-area/homepage?thesky=blue.
8. The user's browser follows the redirect and requests http://mysite.com/new-site-area/homepage?thesky=blue.

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

## Other goodies ##
##### How to redirect other extensions #####
With thanks to RyanEaves, [https://github.com/RyanEaves](https://github.com/RyanEaves) - originally from [https://github.com/thecadams/301RedirectModule/issues/3](https://github.com/thecadams/301RedirectModule/issues/3)

I needed to use this to migrate from a PHP based site, and it took some work to figure out how. So I thought I would share the knowledge. These instructions are for IIS6, although I'm sure you can derive how to do it in IIS7 easily.

1. Add the extension mapping in IIS for your site.

    Go to properties for your site, Home Directory, Configuration button, Mappings tab, Add...

    - Executable: `C:\WINDOWS\Microsoft.NET\Framework\v4.0.30319\aspnet_isapi.dll`

        Note: The above path will be different for you if you use x64 or a different version of .net. Just use the same path that is mapped to the .aspx extension and it will work.

    - Extension: `.php`
    - All Verbs
    - Script engine: checked
    - Verify that file exists: UNCHECKED (very important)

    Click OK

2. Tell Sitecore to process php files:

    - Open Web.config
    - Do a search for: `Sitecore.Pipelines.PreProcessRequest.FilterUrlExtensions`
    - Underneath that line, there is a parameter for "Allowed extensions". Add php to the list.

DONE! Now you can add rules to this module that redirect .php files.

You can do this exact same process for other extension types like .html, .jpg/.gif/.png, .pdf, etc as well if you need to redirect those.

Hope that saves someone a few hours of research!
