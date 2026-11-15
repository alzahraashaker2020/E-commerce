# دليل المستخدم العربي

Open `MATJARK_USER_GUIDE_AR.html` in a browser. It is self-contained, including the four illustrative demo screenshots, and requires no local server or internet to read. Application links still require the application to be running. Search, role filtering, a mobile contents menu and full-guide print/PDF export are included. No account passwords or private customer records are included.

`USER_GUIDE_AR.md` is the maintained editable text. Run `build_guide.py` with Python 3 to regenerate the HTML. It uses only the Python standard library and the existing `docs/previews` images. No application code or data is changed.

The 26 chapters were checked against Angular routes/templates, the order status transition policy, returns/account policies and the current SQL Server deployment notes on 2026-11-08. It explicitly distinguishes customer/guest, merchant, platform administrator and standalone owner access; deferred payments/shipping automation are not documented as available.

Readability checks used Chrome at desktop 1440×1100 and mobile 390×844. Search, merchant filtering, mobile table containment, contents navigation, all 26 chapters in print mode, embedded images and JavaScript errors were checked. Temporary screenshots and test scripts are ignored under `qa/`. The screenshots included in the deliverable are existing saved demo examples, not newly captured current-account screens.
