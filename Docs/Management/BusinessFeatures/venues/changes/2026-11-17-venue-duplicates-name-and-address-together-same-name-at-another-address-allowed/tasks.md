# Tasks: Venue duplicates are name and address together

- [x] VD1 **Repository: `NameAndAddressExistsAsync`.** (REQ-VEN-41 to 44) Level B.
  - Files owned: `Domain/RepositoryInterface/IVenueRepository.cs`, `Infra/Repository/VenueRepository.cs`, `MyVocaList.Tests/Integration/Repositories/VenueRepositoryAddressTests.cs`.
  - Demo: the pair query answers true only when name and all six address fields are equal.
- [x] VD2 **Service: one pair check in every save path.** After VD1. (REQ-VEN-41 to 45) Level A.
  - Files owned: `Services/VenueService.cs`, `MyVocaList.Tests/Unit/Services/VenueServiceSaveTests.cs`, `MyVocaList.Tests/Unit/Services/VenueServiceTests.cs`.
  - Demo: the same name at another address saves; the same name at the same address is refused on the Name field.
- [ ] VD3 Full test run, Android build, merge to `develop`.
- [ ] VD4 **Device check (Helder)** — `design.md § Device check`.
