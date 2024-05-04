import { TestBed } from '@angular/core/testing';

import { StandardSnackbarService } from './standard-snackbar.service';

describe('StandardSnackbarService', () => {
  let service: StandardSnackbarService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(StandardSnackbarService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
