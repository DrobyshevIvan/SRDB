import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ProceduresTab } from './procedures-tab';

describe('ProceduresTab', () => {
  let component: ProceduresTab;
  let fixture: ComponentFixture<ProceduresTab>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProceduresTab]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ProceduresTab);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
