import { TestBed } from '@angular/core/testing';
import { CanActivateFn } from '@angular/router';

import { authenticateGuard } from './authenticate.guard';

describe('authenticateGuard', () => {
  const executeGuard: CanActivateFn = (...guardParameters) => 
      TestBed.runInInjectionContext(() => authenticateGuard(...guardParameters));

  beforeEach(() => {
    TestBed.configureTestingModule({});
  });

  it('should be created', () => {
    expect(executeGuard).toBeTruthy();
  });
});
